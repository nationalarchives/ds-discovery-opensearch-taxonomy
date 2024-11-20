using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class ActiveMqUpdateSender : IUpdateStagingQueueSender, IDisposable
    {
        private readonly ConnectionFactory _activeMqConnectionFactory;
        private readonly IConnection _activeMqConnection;
        private readonly ISession _activeMqSession;
        private readonly IDestination _activeMqdestination;
        private readonly IMessageProducer _activeMqProducer;

        private BlockingCollection<IaidWithCategories> _blockingCollection = new BlockingCollection<IaidWithCategories>();
        private CancellationToken _token = default;

        private readonly int _workerCount;
        private readonly int _batchSize;

        private readonly int _maxSendErrors;
        private List<string> _sendErrors = new List<string>();

        private TaskCompletionSource<bool> _tcs; 

        private volatile int _resultsSent;

        Action<int, int> _updateQueueProgress;

        private ILogger<IUpdateStagingQueueSender> _logger;
        private bool _verboseLoggingEnabled;

        private ThreadLocal<int> _workerResultCount = new ThreadLocal<int>();
        private ThreadLocal<int> _workerMessageCount = new ThreadLocal<int>();

        private bool _initialised;

        public ActiveMqUpdateSender(UpdateStagingQueueParams qParams, ILogger<IUpdateStagingQueueSender> logger)
        {
            if (qParams == null || String.IsNullOrEmpty(qParams.QueueName) || String.IsNullOrEmpty(qParams.Uri))
            {
                throw new TaxonomyException(TaxonomyErrorType.JMS_EXCEPTION, "Invalid or missing queue parameters for Active MQ");
            }

            try
            {
                _activeMqConnectionFactory = new ConnectionFactory(qParams.Uri);
                if (!String.IsNullOrWhiteSpace(qParams.UserName) && !String.IsNullOrWhiteSpace(qParams.Password))
                {
                    _activeMqConnection = _activeMqConnectionFactory.CreateConnection(qParams.UserName, qParams.Password);
                }
                else
                {
                    _activeMqConnection = _activeMqConnectionFactory.CreateConnection();
                }
                _activeMqConnection.Start();
                _activeMqSession = _activeMqConnection.CreateSession(AcknowledgementMode.AutoAcknowledge);
                _activeMqdestination = _activeMqSession.GetQueue(qParams.QueueName);
                _activeMqProducer = _activeMqSession.CreateProducer(_activeMqdestination);

                _workerCount = Math.Max(qParams.WorkerCount, 1);
                _maxSendErrors = qParams.MaxErrors;
                _batchSize = Math.Max(qParams.BatchSize, 1);

                _logger = logger;
                _verboseLoggingEnabled = qParams.EnableVerboseLogging;
            }
            catch (Exception e)
            {
                Dispose();
                throw new TaxonomyException(TaxonomyErrorType.JMS_EXCEPTION, $"Error establishing a connection to ActiveMQ {qParams.QueueName}, at {qParams.Uri}", e);
            }
        }

        public Task<bool> Init(CancellationToken token, Action<int, int> updateQueueProgress)
        {
            if(_initialised)
            {
                return null;
            }

            _token = token;
            _updateQueueProgress = updateQueueProgress;
            _tcs = new TaskCompletionSource<bool>();

            Timer notificationTimer = new Timer(PrintUpdate, null, 60000, 60000) ;

            var tasks = new List<Task>();

            try
            {
                for (int i = 0; i < _workerCount; i++)
                {
                    Task task = Task.Factory.StartNew(Consume1);
                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());
                _tcs.SetResult(_sendErrors.Count == 0 ? true : false);
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
            finally
            {
                notificationTimer?.Dispose();
            }

            _initialised = true;
            return _tcs.Task;
        }

        private  void PrintUpdate(object data)
        {
            if (_resultsSent > 0)
            {
                _updateQueueProgress(_resultsSent, _blockingCollection.Count); 
            }
        }

        public void CompleteAdding()
        {
            try
            {
                _blockingCollection.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public bool IsAddingCompleted
        {
            get => _blockingCollection.IsAddingCompleted;
        }

        public bool Enqueue(IaidWithCategories item, CancellationToken token = default(CancellationToken))
        {


            if (item == null || token.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                _blockingCollection.Add(item);
                return true;
            }
            catch (Exception ex)
            {
                _sendErrors.Add($"Error adding item to internal queue: {item.ToString()}, {ex.Message}");
                return false;
            }
        }

        public IReadOnlyCollection<string> QueueUpdateErrors
        {
            get => new ReadOnlyCollection<string>(_sendErrors);
        }



        private void Consume1()
        {

            while (!IsComplete() && !_token.IsCancellationRequested)
            {
                if (_sendErrors.Count >= _maxSendErrors)
                {
                    if (!_tcs.Task.IsFaulted) //Only one worker should set this as calling repeatedly causes an exception
                    {
                        _tcs.TrySetException(new TaxonomyException(TaxonomyErrorType.JMS_EXCEPTION, "The Active MQ update error count has been exceeded.")); 
                    }
                    CompleteAdding();
                    break;
                }

                var currentBatch = new List<IaidWithCategories>(_batchSize);

                for (int i = 0; i < _batchSize && (!IsComplete() && !_token.IsCancellationRequested); i++)
                {
                    IaidWithCategories nextResult;

                    bool gotResult = _blockingCollection.TryTake(out nextResult); 

                    if(gotResult)
                    {
                        currentBatch.Add(nextResult);
                    }
                }

                if (currentBatch.Count > 0)
                {
                    byte[] serialisedResults = currentBatch.ToByteArray();

                    try
                    {
                        var bytesMessage = _activeMqProducer.CreateBytesMessage(serialisedResults);
                        _activeMqProducer.Send(bytesMessage);
                        _workerMessageCount.Value++;
                        _workerResultCount.Value += currentBatch.Count;
                        _resultsSent += currentBatch.Count;
                        if (_verboseLoggingEnabled)
                        {
                            _logger.LogInformation($"Forwarded a message with {currentBatch.Count} categoriation results to the external ActiveMQ update queue.  This worker [thread ID {Thread.CurrentThread.ManagedThreadId}] has now forwarded {_workerMessageCount.Value} messages containing {_workerResultCount.Value} results."); 
                        }
                    }
                    catch (Exception ex)
                    {
                        _sendErrors.Add($"Error updating the queue for {String.Join(";", currentBatch)}. Details: {ex.Message}");
                    } 
                }
            }

            if(_token.IsCancellationRequested)
            {
                _logger.LogInformation($"Queue update worker [{Thread.CurrentThread.ManagedThreadId}] terminating following a cancellation request.");
                _tcs.TrySetCanceled(); 
                CompleteAdding();
            }
            else
            {
                _logger.LogInformation($"Queue update worker with thread ID [{Thread.CurrentThread.ManagedThreadId}] is finishing as there are no more results on the internal queue.  This worker forwarded {_workerMessageCount.Value} messages containing {_workerResultCount.Value} results.");
            }
        }

        public void Dispose()
        {
            try
            {
                if (_blockingCollection.IsAddingCompleted)
                {
                    CompleteAdding();
                }
             }
            catch (ObjectDisposedException)
            {
            }

            _blockingCollection.Dispose();
            _activeMqProducer?.Dispose();
            _activeMqSession?.Dispose();
            _activeMqConnection?.Dispose();

        }

        private bool IsComplete()
        {
            try
            {
                bool isComplete = _blockingCollection.IsCompleted;
                return isComplete;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
