using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.FullReindex.Producers;
using NationalArchives.Taxonomy.Batch.FullReindex.Queues;
using NationalArchives.Taxonomy.Batch.Properties;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.Service
{
    internal class FullReindexService : BackgroundService
    {
        private readonly ICategoriserService<CategorisationResult> _categoriserService;
        private readonly ILogger<FullReindexService> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly FullReindexIaidProducer _iaidsFromElasticProducer;
        private readonly FullReIndexIaidPcQueue<string> _reindexIaidQueue;
        private readonly IUpdateStagingQueueSender _updateStagingQueueSender;

        private readonly int _categoriserStartDelay;
        private readonly uint _batchSize;
        private readonly uint _numberOfBatchesToProcessConcurrently;
        private readonly bool _logIndividualCategorisationResults;
        private readonly int _taxonomyExceptionThreshold;

        private volatile int _categorisationCount;

        private DateTime _categorisationStartTime;

        private bool _stopped;
        private string _StopMessage;

        public FullReindexService(FullReindexIaidProducer iaidProducer, 
            FullReIndexIaidPcQueue<string> reindexIaidQueue, 
            ICategoriserService<CategorisationResult> categoriserService, 
            IUpdateStagingQueueSender updateStagingQueueSender,
            CategorisationParams catParams,
            ILogger<FullReindexService> logger,
            IHostApplicationLifetime hostApplicationLifetime)
        {

            _categoriserService = categoriserService;
            _logger = logger;

            _categoriserStartDelay = catParams.CategoriserStartDelay;
            _logIndividualCategorisationResults = catParams.LogEachCategorisationResult;
            _iaidsFromElasticProducer = iaidProducer;

            _reindexIaidQueue = reindexIaidQueue;

            _batchSize = catParams.BatchSize;

            _numberOfBatchesToProcessConcurrently = catParams.CategorisationBatchConcurrency;

            _taxonomyExceptionThreshold = catParams.TaxonomyExceptionThreshold;

            _updateStagingQueueSender = updateStagingQueueSender;

            _hostApplicationLifetime = hostApplicationLifetime;
        }



        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _updateStagingQueueSender?.Dispose();
            base.StopAsync(cancellationToken);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _updateStagingQueueSender?.Dispose();
            base.Dispose();
        }

        private void FullReindexElasticFetch_ProcessingCompleted(object sender, MessageProcessingEventArgs e)
        {
            _logger.LogInformation("Elastic search fetch completed.");
        }

        public void IncrementCategorisationCount(int newResults)
        {
           _categorisationCount += newResults;

           TimeSpan runingTime = DateTime.Now - _categorisationStartTime;
           int totalMinutes = (int)Math.Floor(runingTime.TotalMinutes);
           int totalSeconds = (int)Math.Round(runingTime.Seconds * 0.6, 2);
           string pluralMinutes = totalMinutes == 1 ? String.Empty : "s";
           string pluralSeconds = totalSeconds == 1 ? String.Empty : "s";
           _logger.LogInformation($"***** {_categorisationCount} documents now categorised after {totalMinutes} minute{pluralMinutes} {totalSeconds} second{pluralSeconds}. *****"); 

        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<Task> tasks = new List<Task>();

            _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
            _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

            try
            {

                if (_batchSize == 0 || _numberOfBatchesToProcessConcurrently == 0)
                {
                    throw new TaxonomyException($"Both batch size and number of batches to process concurrently must be greater than zero.  The current configured values are {_batchSize} and {_numberOfBatchesToProcessConcurrently} respectively.");
                }

                Action<int, int> updateQueueProgress = (i, j) => _logger.LogInformation($"{i} assets processed and taxonomy results send to the external update queue.  There are currently {j} taxonomy results in the internal update queue.");

                Task iaidProducerTask = _iaidsFromElasticProducer.InitAsync(stoppingToken);
                tasks.Add(iaidProducerTask);
                TaskAwaiter iaidFetchawaiter = iaidProducerTask.GetAwaiter();
                iaidFetchawaiter.OnCompleted(() =>
                {
                    if (iaidProducerTask.Exception != null)
                    {
                        string msg = "Error retrieving Information assets from Elastic Search. Please check the logs for errors.";
                        _StopMessage = msg;
                        _logger.LogError(msg);
                        _logger.LogError(iaidProducerTask.Exception.Message);
                        LogInnerExceptions(iaidProducerTask.Exception.InnerExceptions);
                        //foreach (Exception inner in iaidProducerTask.Exception.InnerExceptions)
                        //{
                        //    //_logger.LogError($"Message: { iaidProducerTask.Exception.Message}, stack trace: { iaidProducerTask.Exception.StackTrace}");
                        //    LogInnerExceptions()
                        //}
                        StopApplication();
                    }
                    else if (iaidProducerTask.IsCanceled)
                    {
                        _logger.LogInformation("Fetch of information asset IDs from Elastic search was cancelled.  Please check the logs for errors.");
                    }
                    else
                    {
                        _logger.LogInformation($"Completed fetch of asset identifiers from Elastic Search. {_iaidsFromElasticProducer.TotalIdentifiersFetched} IAIDs were fetched, current queue size is {_iaidsFromElasticProducer.CurrentQueueSize}");
                    }

                }
                );

                void LogInnerExceptions(IEnumerable<Exception> innerExceptions)
                {
                    foreach (Exception inner in innerExceptions)
                    {
                        Exception current = inner;
                        _logger.LogError($"Message: {current.Message}, stack trace: {current.StackTrace}");
                        while(current.InnerException != null)
                        {
                            current = current.InnerException;
                            _logger.LogError($"Message: {current.Message}, stack trace: {current.StackTrace}");

                        }
                    }
                    
                }

                // wait a while to allow some iaids to be fetched from Elastic (set this via appsettings as desired). 
                // NOw we can start the staging queue, followed



                if (_categoriserStartDelay >= 0)
                {
                    Thread.Sleep((int)_categoriserStartDelay);

                    // Start the update staging queue processor.
                    Task<bool> resultsQueueUpdateTask = Task.Run(() => _updateStagingQueueSender.Init(stoppingToken, updateQueueProgress), stoppingToken);
                    tasks.Add(resultsQueueUpdateTask);
                    TaskAwaiter<bool> resultsQueueAwaiter = resultsQueueUpdateTask.GetAwaiter();
                    resultsQueueAwaiter.OnCompleted(() =>
                    {
                        if (resultsQueueUpdateTask.IsFaulted)
                        {
                            string msg = "Error updating the results queue. Please check the logs for errors";
                            _StopMessage = msg;
                            _logger.LogError(msg);
                            LogInnerExceptions(resultsQueueUpdateTask.Exception.InnerExceptions);
                            //foreach (Exception inner in resultsQueueUpdateTask.Exception.InnerExceptions)
                            //{
                            //    _logger.LogError($"Message: { resultsQueueUpdateTask.Exception.Message}, stack trace: { resultsQueueUpdateTask.Exception.StackTrace}");
                            //}
                            StopApplication();
                        }

                        else if (resultsQueueUpdateTask.IsCanceled)
                        {
                            _StopMessage = "Sending updates of taxonomy results to the destination queue was cancelled. Please check the logs for errors.";
                            _logger.LogInformation(_StopMessage);
                            StopApplication();
                        }
                        else  // success
                        {
                            bool success = resultsQueueAwaiter.GetResult();

                            if (success)
                            {
                                _logger.LogInformation("Finished Updating the destination queue with the categorisation results.");
                            }
                            else
                            {
                                _logger.LogError("Finished Updating the destination queue with the categorisation results, but one or more errors occurred:");
                                foreach (string s in _updateStagingQueueSender.QueueUpdateErrors)
                                {
                                    _logger.LogError(s);
                                }
                            }
                        }
                    }
                    );



                    // Start the full reindex categorisation
                    _categorisationStartTime = DateTime.Now;

                    var fullReindexCategoriser = new FullReindexCategoriser
                        (
                            this,
                            _reindexIaidQueue, _categoriserService, _updateStagingQueueSender,
                            _logger, _batchSize,  _numberOfBatchesToProcessConcurrently,
                            _logIndividualCategorisationResults, _taxonomyExceptionThreshold
                        );

                    _logger.LogInformation("Starting Full Reindex operation.");
                    Task fullRindexTask = fullReindexCategoriser.InitAsync(stoppingToken);
                    tasks.Add(fullRindexTask);
                    TaskAwaiter reIndexawaiter = fullRindexTask.GetAwaiter();
                    reIndexawaiter.OnCompleted
                    (
                        () =>
                        {

                            if (fullRindexTask.IsFaulted)
                            {
                                foreach (Exception inner in fullRindexTask.Exception.InnerExceptions)
                                {
                                    _logger.LogError($"Message: { fullRindexTask.Exception.Message}, stack trace: { resultsQueueUpdateTask.Exception.StackTrace}");
                                }
                                _StopMessage = "Fatal exception occured during processing the full reindex operation.  Please check the logs for details.";
                                _logger.LogCritical(_StopMessage);
                                StopApplication();

                            }
                            else if (fullRindexTask.IsCanceled)
                            {
                                _StopMessage = "Fetching and classification of information assets for full reindexing has been cancelled.  Please check the log for errors.";
                                _logger.LogInformation(_StopMessage);
                                StopApplication();

                            }
                            else
                            {
                                _updateStagingQueueSender.CompleteAdding();
                                _logger.LogInformation("Full Reindexing categorisation process completed successfully.  There may still be some results not yet written to the external update queue.  Please continue to check the logs.");
                            }
                        }
                    );
                }
                else
                {
                    _logger.LogInformation(Resources.CATEGORISATION_DISABLED_MSG);
                    _StopMessage = Resources.CATEGORISATION_DISABLED_MSG;
                    StopApplication();
                }

                return Task.CompletedTask;
            }

            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
                _logger.LogCritical("Fatal exception occured during processing, please check the logs for details.");
                _logger.LogCritical("Cancelling document feed from elastic search following exception");
                StopApplication();
                return Task.FromException(e);
            }
            finally
            {
                TaskAwaiter awaiterAll = Task.WhenAll(tasks).GetAwaiter();
                awaiterAll.OnCompleted(() => { StopApplication(); });
            }
        }

        public int CategorisationCount
        {
            get => _categorisationCount;
        }

        public string StopMessage
        {
            get => _StopMessage;
        }

        private void StopApplication()
        {
            if(!_stopped)
            {
                _stopped = true;
                _hostApplicationLifetime.StopApplication();
            }
        }


        private void OnStarted()
        {
            _logger.LogInformation("Taxonomy Generator Full Reindex Service has started.");

        }

        private void OnStopping()
        {
            StringBuilder sb = new StringBuilder("Taxonomy Generator Full Reindex Service is stopping.");

            if(!String.IsNullOrWhiteSpace(StopMessage))
            {
                sb.Append("\n");
                sb.Append(StopMessage);
            }
            _logger.LogInformation(sb.ToString());
        }

        private void OnStopped()
        {
            _logger.LogInformation("Taxonomy Generator Full Reindex Service is stopped.");
        }
    }
}
