﻿using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using NationalArchives.Taxonomy.Common.Helpers;
using NationalArchives.Taxonomy.Common.Service.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Service.Impl
{
    public class UpdateOpenSearchService : IUpdateOpenSearchService, IDisposable
    {
        private readonly IUpdateStagingQueueReceiver<IaidWithCategories> _interimUpdateQueue;
        private readonly IOpenSearchIAViewUpdateRepository _targetOpenSearchRepository;
        private readonly ConcurrentQueue<IaidWithCategories> _internalQueue = new ConcurrentQueue<IaidWithCategories>();
        private readonly int _batchSize;
        private readonly int _queueFetchSleepTimeMS;
        private readonly int _queueFetchWaitTimeMS;
        private readonly int _searchDatabaseUpdateIntervalMS;
        private readonly int _maxInternalQueueSize;
        private readonly ILogger _logger;

        //private const int NULL_COUNTER_THRESHOLD = 259200;  // 259200 seconds == 3 days.
        private const int NULL_COUNTER_THRESHOLD = 72;  // Keep running for 3 days with 1 check per hour
        private const int MAX_SEARCH_DB_UPDATE_ERRORS = 5;

        bool _isProcessingComplete = false;

        private volatile int _totalInfoAssetsUpdated;

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();
        private int _searchDatabaseUpdateErrors = 0;

    public bool IsProcessingComplete { get => _isProcessingComplete; set => _isProcessingComplete = value; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateQueue"></param>
        /// <param name="targetOpenSearchRepository">OpenSearch repo</param>
        /// <param name="logger">Logger</param>
        /// <param name="batchSize">Batch size for sending results to OpenSearch via bulk update</param>
        /// <param name="queueFetchWaitTimeMS">Time to wait for a response when requesting messages from the queue</param>
        /// <param name="queueFetchSleepTimeMS">Time to wait after processing one set of results to query the queue for the next set</param>
        /// <param name="searchDatabaseUpdateIntervalMS">Interval at which to check the internal queue for updates received from SQS and to submit them as a batch to OpenSearch</param>
        /// <param name="maxInternalQueueSize"></param>
        /// <exception cref="TaxonomyException"></exception>
        public UpdateOpenSearchService(IUpdateStagingQueueReceiver<IaidWithCategories> updateQueue, IOpenSearchIAViewUpdateRepository targetOpenSearchRepository, ILogger logger, 
            int batchSize, int queueFetchWaitTimeMS, int queueFetchSleepTimeMS, int searchDatabaseUpdateIntervalMS, int maxInternalQueueSize)
        {
            if (updateQueue == null || targetOpenSearchRepository == null)
            {
                throw new TaxonomyException("Input queue and target Open Search repository are required.");
            }

            _interimUpdateQueue = updateQueue;
            _targetOpenSearchRepository = targetOpenSearchRepository;
            _batchSize = batchSize;
            _queueFetchSleepTimeMS = queueFetchSleepTimeMS;
            _queueFetchWaitTimeMS = queueFetchWaitTimeMS;
            _searchDatabaseUpdateIntervalMS = searchDatabaseUpdateIntervalMS;
            _maxInternalQueueSize = maxInternalQueueSize;
            _logger = logger;
        }

        public async Task Init()
        {
            try
            {
                await StartProcessing(_cancelSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception whilst starting or running the Taxonomy Search database update process.");
                throw;
            }
        }

        public async Task Flush()
        {
            List<IaidWithCategories> remainingInternalQueueItems = null;

            try
            {
                do
                {
                    remainingInternalQueueItems = _internalQueue.DequeueChunk<IaidWithCategories>(_batchSize).ToList();
                    if (remainingInternalQueueItems.Count > 0)
                    {
                        await BulkUpdateCategoriesOnIAViews(remainingInternalQueueItems);
                    }
                } while (remainingInternalQueueItems.Count() > 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception Occurred whilst flushing remaining updates from the internal queue.");
                throw;
            }
            finally
            {
                IsProcessingComplete = true;
            }
        }

        private async Task StartProcessing(CancellationToken token)
        {
            int nullCounter = 0;

            try
            {

                Task searchDatabaseUpdateTask = Task.Run(() => PeriodicSearchDatabaseUpdateAsync(TimeSpan.FromMilliseconds(_searchDatabaseUpdateIntervalMS), token));

                while (!IsProcessingComplete && !_cancelSource.IsCancellationRequested)
                {
                    List<IaidWithCategories> nextBatchOfResults = null;

                    if (_internalQueue.Count < _maxInternalQueueSize)
                    {
                       nextBatchOfResults = await _interimUpdateQueue.GetNextBatchOfResults(_logger, sqsRequestTimeoutMilliSeconds: _queueFetchWaitTimeMS); 
                    }
                    else
                    {
                        // wait for some more database updates to reduce the internal queue size

                        do
                        {
                            int latestIniternalQueueSize = _internalQueue.Count;
                            _logger.LogInformation($"Pausing fetch from SQS as the internal queue size is currently {latestIniternalQueueSize}. The configured maximum is {_maxInternalQueueSize}.");
                            _logger.LogInformation($"Internal queue needs to reach target size of {_maxInternalQueueSize / 2} before resuming fetches from SQS. Will check again in 5 minutes for search database updates to reduce the queue");

                            await Task.Delay(TimeSpan.FromMinutes(5));

                            if (_internalQueue.Count >= latestIniternalQueueSize)
                            {
                                throw new Exception("Internal queue reached or exceeded the maximum size. It has not reduced despite pausing fetches from SQS to allow for database updates.  This indicates a possible issue with the database update process");
                            }
                        } while (_internalQueue.Count > (_maxInternalQueueSize / 2));

                        _logger.LogInformation($"Internal queue size is now {_internalQueue.Count}.  Resuming fetch of taxonomy results from SQS.");
                        nextBatchOfResults = await _interimUpdateQueue.GetNextBatchOfResults(_logger, sqsRequestTimeoutMilliSeconds: Math.Min(_queueFetchWaitTimeMS, 20000));
                    }

                    if (nextBatchOfResults != null)
                    {

                        if (nextBatchOfResults?.Count > 0)
                        {
                            foreach (IaidWithCategories categorisationResult in nextBatchOfResults)
                            {
                                if (categorisationResult != null)
                                {
                                    _internalQueue.Enqueue(categorisationResult);
                                }
                            }
                            await Task.Delay(_queueFetchSleepTimeMS);
                        }
                        else
                        {
                            nullCounter++;

                            // If we didn;t get anything back, Wait an hour before trying again...
                            await Task.Delay(TimeSpan.FromHours(1));

                            // this allows us to keep running for 3 days with no updates before shutting down the service, assuming a one hour wait between each check.
                            if (nullCounter >= NULL_COUNTER_THRESHOLD)
                            {
                                IsProcessingComplete = true;
                                await RetrieveAndSubmitUpdatesToOpenSearchDatabase();
                                _cancelSource.Cancel();
                                _logger.LogInformation("No more categorisation results found on update queue.  Open Search Update service will now finish processing.");
                            }
                        }
                    }
                } 
                    
                await Task.Delay(_queueFetchSleepTimeMS);

            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                _cancelSource?.Cancel();
            }
        }

        private async Task PeriodicSearchDatabaseUpdateAsync(TimeSpan interval,  CancellationToken cancellationToken)
        {
           DateTime _lastOpenSearchUpdateTime = DateTime.Now;
           int minutesSinceLastNoUpdatesLogMessage = 0;
           TimeSpan timeSinceLastOpenSearchUpdateCheck = interval;

            try
            {
                while (true && !cancellationToken.IsCancellationRequested)
                {
                    if (_internalQueue.Count >= _batchSize || ((_internalQueue.Count > 0) && timeSinceLastOpenSearchUpdateCheck >= interval))
                    {
                        Task delayTask = Task.Delay(interval, cancellationToken);
                        await RetrieveAndSubmitUpdatesToOpenSearchDatabase();
                        
                        await delayTask;

                    }
                    else
                    {
                        if (timeSinceLastOpenSearchUpdateCheck >= TimeSpan.FromMinutes(5))
                        {
                            
                            int minutesSinceLastUpdate = Convert.ToInt32(Math.Round(timeSinceLastOpenSearchUpdateCheck.TotalMinutes));
                            if (minutesSinceLastUpdate % 5 == 0 && minutesSinceLastUpdate > minutesSinceLastNoUpdatesLogMessage)
                            {
                                minutesSinceLastNoUpdatesLogMessage = minutesSinceLastUpdate;
                                _logger.LogInformation($"No Taxonomy updates have been received by the Open Search" +
                                    $" update service in the last {minutesSinceLastUpdate} minutes.  Resetting the update counter.");
                                _totalInfoAssetsUpdated = 0;
                            }
                        }
                    }

                    timeSinceLastOpenSearchUpdateCheck = DateTime.Now - _lastOpenSearchUpdateTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error updating OpenSearch database Taxonomy records.");
                _searchDatabaseUpdateErrors++ ;

                if (_searchDatabaseUpdateErrors >= MAX_SEARCH_DB_UPDATE_ERRORS)
                {
                    _logger.LogCritical($"Taxonomy search database update has exceeded the configured error threshold of {MAX_SEARCH_DB_UPDATE_ERRORS}. Operation aborted.");
                    _cancelSource.Cancel();
                }

            }
        }

        private async Task RetrieveAndSubmitUpdatesToOpenSearchDatabase()
        {
            if (_batchSize == 1 || _internalQueue.Count == 1)
            {
                IaidWithCategories nextItem;
                bool itemRetrived = _internalQueue.TryDequeue(out nextItem);
                if (itemRetrived)
                {
                    await UpdateCategoriesOnIAView(nextItem); 
                }
            }
            else
            {
                var items = _internalQueue.DequeueChunk<IaidWithCategories>(_batchSize).ToList();
                await BulkUpdateCategoriesOnIAViews(items);
            }
        }

        private async Task BulkUpdateCategoriesOnIAViews(IList<IaidWithCategories> listOfIAViewUpdatesToProcess)
        {

            if(listOfIAViewUpdatesToProcess.Count == 0)
            {
                return;
            }

            try
            {
                _logger.LogInformation($"Submitting bulk update of {listOfIAViewUpdatesToProcess.Count} items to Open Search: ");
                await _targetOpenSearchRepository.SaveAll(listOfIAViewUpdatesToProcess);

                foreach (var item in listOfIAViewUpdatesToProcess)
                {
                    _logger.LogInformation($"Updated Open Search entry: {item.ToString()}".PadLeft(5));
                }

                int totalForThisBulkUpdateOperation = listOfIAViewUpdatesToProcess.Count;
                _logger.LogInformation($"Completed bulk update in Open Search for {totalForThisBulkUpdateOperation} items: ");
                _totalInfoAssetsUpdated += totalForThisBulkUpdateOperation;
                _logger.LogInformation($" Category data for {_totalInfoAssetsUpdated} assets has now been added or updated in Open Search.");
                _logger.LogInformation($" There are currently {_internalQueue.Count} results on the internal queue that have been retrieved from Amazon SQS and are awaiting submission to the database.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task UpdateCategoriesOnIAView(IaidWithCategories item)
        {
            try
            {
                _logger.LogInformation("Submitting single Asset update to Open Search: " + item.ToString());
                await _targetOpenSearchRepository.Save(item);
                _logger.LogInformation($"Completed single Asset in Open Search: {item.ToString()}." );
                _totalInfoAssetsUpdated++;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void Dispose()
        {
            _cancelSource?.Dispose();
        }
    }
}
