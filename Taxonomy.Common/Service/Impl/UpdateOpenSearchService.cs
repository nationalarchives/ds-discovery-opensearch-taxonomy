using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using NationalArchives.Taxonomy.Common.Helpers;
using NationalArchives.Taxonomy.Common.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NationalArchives.Taxonomy.Common.Service.Impl
{
    public class UpdateOpenSearchService : IUpdateOpenSearchService
    {
        private readonly IUpdateStagingQueueReceiver _interimUpdateQueue;
        private readonly IOpenSearchIAViewUpdateRepository _targetOpenSearchRepository;
        private readonly Queue<IaidWithCategories> internalQueue = new Queue<IaidWithCategories>();
        private readonly uint _batchSize;
        private readonly int _queueFetchWaitTime;

        private readonly ILogger _logger;

        private const int NULL_COUNTER_THRESHOLD = 259200;  // 259200 seconds == 3 days.

        bool _isProcessingComplete = false;

        private volatile int _totalInfoAssetsUPdated;

        public bool IsProcessingComplete { get => _isProcessingComplete; set => _isProcessingComplete = value; }

        private DateTime _lastOpenSearchUpdate = DateTime.Now;

        public UpdateOpenSearchService(IUpdateStagingQueueReceiver updateQueue, IOpenSearchIAViewUpdateRepository targetOpenSearchRepository, ILogger logger, uint batchSize = 1, uint queueFetchWaitTime = 1000)
        {
            if (updateQueue == null || targetOpenSearchRepository == null)
            {
                throw new TaxonomyException("Input queue and target Open Search repository are required.");
            }

            _interimUpdateQueue = updateQueue;
            _targetOpenSearchRepository = targetOpenSearchRepository;
            _batchSize = batchSize;
            _queueFetchWaitTime = Convert.ToInt32(queueFetchWaitTime);
            _logger = logger;
        }

        public void  Init()
        {
            try
            {
                StartProcessing();
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder("Exception Occurred: " + e.Message);
                sb.Append("\n");
                sb.Append("Stack Trace: \n");
                sb.Append(e.StackTrace);
                _logger.LogError(sb.ToString());
                throw;
            }
        }

        public void Flush()
        {
            List<IaidWithCategories> remainingInternalQueueItems = null;

            try
            {
                do
                {
                    remainingInternalQueueItems = internalQueue.DequeueChunk<IaidWithCategories>(_batchSize).ToList();
                    if (remainingInternalQueueItems.Count > 0)
                    {
                        BulkUpdateCategoriesOnIAViews(remainingInternalQueueItems);
                    }
                } while (remainingInternalQueueItems.Count() > 0);
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder("Exception Occurred whilst flushing remaining updates: " + e.Message);
                sb.Append("\n");
                sb.Append("Stack Trace: \n");
                sb.Append(e.StackTrace);

                _logger.LogError(sb.ToString());
                throw;
            }
            finally
            {
                IsProcessingComplete = true;
            }
        }

        private void StartProcessing()
        {
            int nullCounter = 0;
            int minutesSinceLastNoUpdatesLogMessage = 0;

            try
            {
                while (!IsProcessingComplete)
                {
                    List<IaidWithCategories> nextBatchFromInterimUpdateQueue = _interimUpdateQueue.DeQueueNextListOfIaidsWithCategories();
                    if (nextBatchFromInterimUpdateQueue != null)
                    {
                        foreach (var categorisationResultItem in nextBatchFromInterimUpdateQueue)
                        {
                            if (categorisationResultItem != null)
                            {
                                internalQueue.Enqueue(categorisationResultItem);
                            }
                        } 
                    }
                    else
                    {
                        nullCounter++;
                    }

                    Thread.Sleep(_queueFetchWaitTime);

                    TimeSpan timeSinceLastUpdate = DateTime.Now - _lastOpenSearchUpdate;

                    if (internalQueue.Count >= _batchSize || ((internalQueue.Count > 0) && timeSinceLastUpdate >= TimeSpan.FromMinutes(5)))
                    {
                        _lastOpenSearchUpdate = DateTime.Now;
                        SubmitUpdatesToOpenSearchDatabase();

                    }
                    else
                    {
                        if (timeSinceLastUpdate >= TimeSpan.FromMinutes(5))
                        {
                            _totalInfoAssetsUPdated = 0;
                            int minutesSinceLastUpdate = Convert.ToInt32(Math.Round(timeSinceLastUpdate.TotalMinutes));
                            if (minutesSinceLastUpdate % 5 == 0 && minutesSinceLastUpdate > minutesSinceLastNoUpdatesLogMessage)
                            {
                                minutesSinceLastNoUpdatesLogMessage = minutesSinceLastUpdate;
                                _logger.LogInformation($"No Taxonomy updates have been received by the Open Search" +
                                    $" update service in the last {minutesSinceLastUpdate} minutes.  Resetting the update counter.");
                            } 
                        }
                    }

                    // this allows us to keep running for 3 days with no updates before shutting down the service, given the one second wait between each check.
                    if (nullCounter >= NULL_COUNTER_THRESHOLD)
                    {
                        IsProcessingComplete = true;
                        SubmitUpdatesToOpenSearchDatabase();
                        _logger.LogInformation("No more categorisation results found on update queue.  Open Search Update service will now finish processing.");
                    }

                    void SubmitUpdatesToOpenSearchDatabase()
                    {
                        if (_batchSize == 1 || internalQueue.Count == 1)
                        {
                            UpdateCategoriesOnIAView(internalQueue.Dequeue());
                        }
                        else
                        {
                            var items = internalQueue.DequeueChunk<IaidWithCategories>(_batchSize).ToList();
                            BulkUpdateCategoriesOnIAViews(items);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private void BulkUpdateCategoriesOnIAViews(IList<IaidWithCategories> listOfIAViewUpdatesToProcess)
        {

            if(listOfIAViewUpdatesToProcess.Count == 0)
            {
                return;
            }

            try
            {
                _logger.LogInformation($"Submitting bulk update of {listOfIAViewUpdatesToProcess.Count} items to Open Search: ");
                _targetOpenSearchRepository.SaveAll(listOfIAViewUpdatesToProcess);

                foreach (var item in listOfIAViewUpdatesToProcess)
                {
                    _logger.LogInformation($"Updated Open Search entry: {item.ToString()}".PadLeft(5));
                }

                int totalForThisBulkUpdateOperation = listOfIAViewUpdatesToProcess.Count;
                _logger.LogInformation($"Completed bulk update in Open Search for {totalForThisBulkUpdateOperation} items: ");
                _totalInfoAssetsUPdated += totalForThisBulkUpdateOperation;
                _logger.LogInformation($" Category data for {_totalInfoAssetsUPdated} assets has now been added or updated in Open Search.");
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private void UpdateCategoriesOnIAView(IaidWithCategories item)
        {
            try
            {
                _logger.LogInformation("Submitting single Asset update to Open Search: " + item.ToString());
                _targetOpenSearchRepository.Save(item);
                _logger.LogInformation($"Completed single Asset in Open Search: {item.ToString()}." );
                _totalInfoAssetsUPdated++;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
