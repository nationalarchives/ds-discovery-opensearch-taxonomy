﻿using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsUpdateSender : IUpdateStagingQueueSender, IDisposable
    {
        private const string ROLE_SESSION_NAME = "Taxonomy_SQS_Update_FULL_REINDEX";

        private BlockingCollection<IaidWithCategories> _blockingCollection = new BlockingCollection<IaidWithCategories>();
        private CancellationToken _token = default;

        private readonly int _workerCount;
        private readonly int _batchSize;

        private readonly int _maxSendErrors;
        private List<string> _sendErrors = new List<string>();

        private TaskCompletionSource<bool> _tcs; 

        private volatile int _resultsSent;

        Action<int, int> _updateQueueProgress;

        private readonly ILogger<IUpdateStagingQueueSender> _logger;
        private bool _verboseLoggingEnabled;

        private ThreadLocal<int> _workerResultCount = new ThreadLocal<int>();
        private ThreadLocal<int> _workerMessageCount = new ThreadLocal<int>();

        private readonly AmazonSqsParams _sqsParams;
        private readonly int _sendIntervalMS;

        private bool _initialised;

        public AmazonSqsUpdateSender(UpdateStagingQueueParams updateStagingQueueParams, ILogger<IUpdateStagingQueueSender> logger)
        {
            try
            {
                if (updateStagingQueueParams == null || String.IsNullOrEmpty(updateStagingQueueParams.AmazonSqsParams.QueueUrl))
                {
                    throw new TaxonomyException(TaxonomyErrorType.SQS_EXCEPTION, "Invalid or missing queue parameters for Amazon SQS");
                }

                _sqsParams = updateStagingQueueParams.AmazonSqsParams;
                _workerCount = Math.Max(updateStagingQueueParams.WorkerCount, 1);
                _maxSendErrors = updateStagingQueueParams.MaxErrors;
                _batchSize = Math.Max(updateStagingQueueParams.BatchSize, 1);

                _logger = logger;
                _verboseLoggingEnabled = updateStagingQueueParams.EnableVerboseLogging;
                _sendIntervalMS = updateStagingQueueParams.SendIntervalMilliseconds;
            }
            catch (Exception e)
            {
                throw ;
            }
            // finally { Dispose(); }
        }

        public async Task<bool> Init(CancellationToken token, Action<int, int> updateQueueProgress)
        {
            if(_initialised)
            {
                return false;
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

                var firstToComplete = await Task.WhenAny(tasks);
                await firstToComplete;
                if (_tcs.Task.IsFaulted) 
                {
                    throw _tcs.Task.Exception;
                }

                Task.WaitAll(tasks.ToArray());
                _tcs.SetResult(_sendErrors.Count == 0 ? true : false);
            }
            catch (Exception ex)
            {
                if (!_tcs.Task.IsFaulted)
                {
                    _tcs.SetException(ex); 
                }
                return await _tcs.Task;
            }
            finally
            {
                notificationTimer?.Dispose();
            }

            _initialised = true;
            return await _tcs.Task;
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



        private async Task Consume1()
        {

            while (!IsComplete() && !_token.IsCancellationRequested)
            {
                if (_sendErrors.Count >= _maxSendErrors)
                {
                    if (!_tcs.Task.IsFaulted) //Only one worker should set this as calling repeatedly causes an exception
                    {
                        _tcs.TrySetException(new TaxonomyException(TaxonomyErrorType.SQS_EXCEPTION, "The SQS update error count has been exceeded.")); 
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
                    try
                    {
                        RegionEndpoint region = RegionEndpoint.GetBySystemName(_sqsParams.Region);
                        AWSCredentials credentials = _sqsParams.GetCredentials(ROLE_SESSION_NAME);

                        var request = new SendMessageRequest()
                        {
                            MessageBody = JsonConvert.SerializeObject(currentBatch),
                            QueueUrl = _sqsParams.QueueUrl,
                        };

                        using AmazonSQSClient client = new AmazonSQSClient(credentials, region);
                        SendMessageResponse result =  client.SendMessageAsync(request).Result;

                        if (_sendIntervalMS > 0)
                        {
                            await Task.Delay(_sendIntervalMS); 
                        }

                    }
                    catch (Exception ex)
                    {
                        _tcs.SetException(ex);
                        throw;
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

            _blockingCollection?.Dispose();
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
