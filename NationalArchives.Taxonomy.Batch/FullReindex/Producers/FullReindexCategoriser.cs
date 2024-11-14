using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.FullReindex.Queues;
using NationalArchives.Taxonomy.Batch.Service;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.FullReindex.Producers
{
    internal sealed class FullReindexCategoriser
    {
        private const string IAID_QUEUE_COMPLETE_MSG = "Worker producer iaid queue marked as complete.";
        private const string SOURCE_IAID_QUEUE_BLOCK_MSG = "Take call from Category Worker blocked from source IAID queue.";
        private const string CATEGORISATION_CANCELLED_MSG = "Taking of iaids by categorisation worker cancelled.";
        private const string PROCESSSING_ABORTED_CURRENT_BATCH_MSG = "Processing aborted for the current batch";
        private const string IAIDS_NOT_PROCESSED_MSG = "One or more of the following IAIDs may not be processed: ";
        private const string NO_MORE_IAIDS_MSG = "There are no  more IAIDs on the queue after this batch";

        private readonly FullReIndexIaidPcQueue<string> _sourceIaidQueue;

        private readonly ICategoriserService<CategorisationResult> _categoriserService;
        private readonly CancellationToken _ct;

        private readonly ILogger<FullReindexService> _logger;

        private readonly FullReindexService _parent;
        private static DateTime _categorisationStart;

        private bool _logIndividualCategorisationResults;

        private uint _batchSize;
        private uint _numberOfBatchesToProcessConcurrently;


        private  readonly int _taxonomyExceptionThreshold;
        private readonly IUpdateStagingQueueSender _stagingQueueSender;
        private int _taxonomyExceptionCount;

        static FullReindexCategoriser()
        {
            _categorisationStart = DateTime.Now;
        }

        public FullReindexCategoriser(FullReindexService parent,  FullReIndexIaidPcQueue<string> sourceIaidQueue,  ICategoriserService<CategorisationResult> categoriser, IUpdateStagingQueueSender stagingQueueSender, ILogger<FullReindexService> logger, uint batchSize, uint numberOfBatchesToProcessConcurrently,  bool logCategorisationResults = true, int taxonomyExceptionThreshold = -1)
        {
            //TODO: Don't need _destinationQueue as we just need to categorise and the categoriser service will know about this queue
            _sourceIaidQueue = sourceIaidQueue;
            //_destinationQueue = destinationQueue;
            _categoriserService = categoriser;
            _logger = logger;
            _logIndividualCategorisationResults = logCategorisationResults;
            _parent = parent;
            _batchSize = batchSize;
            _numberOfBatchesToProcessConcurrently = numberOfBatchesToProcessConcurrently;
            _taxonomyExceptionThreshold = taxonomyExceptionThreshold;
            _stagingQueueSender = stagingQueueSender;
        }

        public async Task InitAsync(CancellationToken token)
        {
            try
            {
                switch (_numberOfBatchesToProcessConcurrently)
                {
                    case 0:
                    case 1:
                        await Task.Run(() => ProcessSingleBatch(token), token);
                        break;
                    default:
                        await Task.Run(() => ProcessMultiBatch(_numberOfBatchesToProcessConcurrently, token), token);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        private void ProcessMultiBatch(uint batchSize, CancellationToken token)
        {
            string[][] batches = null;

            List<string> batchedIaids = null;

            _logger.LogInformation($"Started a full reindex operation using multi batch processing with {_numberOfBatchesToProcessConcurrently} concurrent batches with a batch size of {_batchSize}");

            DateTimeOffset lastQueueBlockLogTime = DateTimeOffset.Now;
            TimeSpan lastQueueBlockLogInterval = TimeSpan.FromMinutes(10);

            while (!_sourceIaidQueue.IsCompleted && !token.IsCancellationRequested)
            {
                string nextIaid;
                try
                {
                    batches = new string[batchSize][];



                    for (int i = 0; i < _numberOfBatchesToProcessConcurrently; i++)
                    {
                        batchedIaids = new List<string>();

                        while (batchedIaids.Count < _batchSize && !_sourceIaidQueue.IsCompleted)
                        {
                            if (!_sourceIaidQueue.TryTake(out nextIaid, _ct, 0))
                            {
                                if (DateTimeOffset.Now - lastQueueBlockLogTime > lastQueueBlockLogInterval)
                                {
                                    _logger.LogWarning(SOURCE_IAID_QUEUE_BLOCK_MSG);
                                    lastQueueBlockLogTime = DateTimeOffset.Now;
                                }
                            }
                            else
                            {
                                batchedIaids.Add(nextIaid);
                            }

                            if (_sourceIaidQueue.IsCompleted)
                            {
                                break;
                            }
                        }

                        batches[i] = batchedIaids.ToArray();

                        if (_sourceIaidQueue.IsCompleted)
                        {
                            break;
                        }
                    }

                    int populatedBatches = batches.Count(inner => inner != null);
                    string[][] batchesCopy = new string[populatedBatches][];

                    for (int i = 0; i < populatedBatches; i++)
                    {
                        if (batches[i] != null)
                        {
                            string[] batchCopy = new string[batches[i].Count()];
                            Array.Copy(batches[i], batchCopy, batches[i].Length);
                            batchesCopy[i] = batchCopy;
                        }
                    }

                    batches = null;

                    if(_sourceIaidQueue.IsCompleted)
                    {
                        _logger.LogInformation("Source information asset queue is now marked as completed.  Submitting the final batches for categorisation.");
                    }

                    SubmitForCategorisation(batchesCopy);
                }
                catch (TaxonomyException ex)
                {
                    LogTaxonomyException(ex, batchedIaids);
                    
                    if (_taxonomyExceptionThreshold > 0 && _taxonomyExceptionCount >= _taxonomyExceptionThreshold)
                    {
                        throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_INVALID_RESPONSE, $"Processing cannot continue as the configured taxonomy exception count of {_taxonomyExceptionThreshold} has been reached.", ex);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch(AggregateException ex)
                {
                    if(ex.InnerException.GetType() == typeof(TaxonomyException))
                    {
                        LogTaxonomyException((TaxonomyException)ex.InnerException, batchedIaids);

                        if (_taxonomyExceptionThreshold > 0 && _taxonomyExceptionCount >= _taxonomyExceptionThreshold)
                        {
                            throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_INVALID_RESPONSE, $"Processing cannot continue as the configured taxonomy exception count of {_taxonomyExceptionThreshold} has been reached.", ex);
                        }
                        else
                        {
                            throw ex.InnerException;
                        }
                        
                    }
                    else
                    {
                        LogException(ex.InnerException);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                    throw;
                }
            }

            if (!token.IsCancellationRequested)
            {
                _logger.LogInformation(IAID_QUEUE_COMPLETE_MSG);
            }
            else
            {
                string cancelMessage = "Full reindex single batch processing operation cancelled.  Please check the logs for further errors.";
                throw new OperationCanceledException(cancelMessage, token);
            }
        }

        private void LogTaxonomyException(TaxonomyException ex, IEnumerable<string> batchedIaids)
        {
            _logger.LogError("Taxonomy Error:" + ex.Message);


            Exception inner = ex.InnerException;

            while (inner != null)
            {
                _logger.LogError("Inner Exception: " + inner.Message);
                inner = inner.InnerException;
            }

            _logger.LogError(PROCESSSING_ABORTED_CURRENT_BATCH_MSG);
            _logger.LogError(IAIDS_NOT_PROCESSED_MSG + string.Join(';', batchedIaids));


        }

        private void LogException(Exception ex)
        {
            _logger.LogCritical("Fatal Error:" + ex.Message);

            Exception inner = ex.InnerException;

            while (inner != null)
            {
                _logger.LogCritical("Inner Exception: " + inner.Message);
                inner = inner.InnerException;
            }

            _logger.LogCritical(ex.StackTrace);
        }

        private void ProcessSingleBatch(CancellationToken token)
        {
            List<string> batchedIaids = new List<string>();
            string[] currentBatch = null;

            _logger.LogInformation($"Started a full reindex operation using single batch processing with a batch size of {_batchSize}");
            DateTimeOffset lastQueueBlockLogTime = DateTimeOffset.Now;
            TimeSpan lastQueueBlockLogInterval = TimeSpan.FromMinutes(10);

            while (!_sourceIaidQueue.IsCompleted && !token.IsCancellationRequested)
            {

                string nextIaid;
                try
                {
                    
                    

                    if (!_sourceIaidQueue.TryTake(out nextIaid, _ct, 0))
                    {
                        if (DateTimeOffset.Now - lastQueueBlockLogTime > lastQueueBlockLogInterval)
                        {
                            _logger.LogWarning(SOURCE_IAID_QUEUE_BLOCK_MSG);
                            lastQueueBlockLogTime = DateTimeOffset.Now;
                        }
                    }
                    else
                    {

                        batchedIaids.Add(nextIaid);

                        //Check for completion so we don't wait for a full batch size once there are no more to process.
                        if (batchedIaids.Count % _batchSize == 0 || (_sourceIaidQueue.IsCompleted))
                        {
                            DateTime queryStart = DateTime.Now;

                            _logger.LogInformation($"Submitting a batch of {batchedIaids.Count()} for categorisation.");
                            if (_sourceIaidQueue.IsCompleted)
                            {
                                _logger.LogInformation(NO_MORE_IAIDS_MSG);
                            }

                            var awaiter = _categoriserService.CategoriseMultiple(batchedIaids.ToArray()).GetAwaiter();
                            currentBatch = batchedIaids.ToArray();
                            batchedIaids.Clear();
                            IDictionary<string, List<CategorisationResult>> dictionaryOfMatchingCategories = awaiter.GetResult();
                            DateTime queryEnd = DateTime.Now;
                            TimeSpan queryTime = queryEnd - queryStart;


                            _logger.LogInformation($" Finished categorising {dictionaryOfMatchingCategories.Count} assets. Operation took {Math.Round(queryTime.TotalSeconds, 5)} seconds.");
                            _parent.IncrementCategorisationCount((int)_batchSize);
                            if (_logIndividualCategorisationResults)
                            {
                                foreach (var matchingCategoryList in dictionaryOfMatchingCategories)
                                {
                                    int numberOfCategoriesMatched = matchingCategoryList.Value.Count;
                                    _logger.LogInformation($"  - Categorised {matchingCategoryList.Key}, {numberOfCategoriesMatched} " + (numberOfCategoriesMatched == 1 ? "category" : "categories") + $" found: " + String.Join(';', matchingCategoryList.Value));
                                }
                            }
                        }
                    }
                    currentBatch = null;
                }
                catch (TaxonomyException ex)
                {
                    _logger.LogError("Taxonomy Error:" + ex.Message);


                    Exception inner = ex.InnerException;

                    while (inner != null)
                    {
                        _logger.LogCritical("Inner Exception: " + inner.Message);
                        inner = inner.InnerException;
                    }

                    _logger.LogError(PROCESSSING_ABORTED_CURRENT_BATCH_MSG);
                    _logger.LogError(IAIDS_NOT_PROCESSED_MSG+ string.Join(';', currentBatch));

                    _taxonomyExceptionCount++;

                    if(_taxonomyExceptionThreshold > 0 && _taxonomyExceptionCount >= _taxonomyExceptionThreshold)
                    {
                        throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_INVALID_RESPONSE, $"Processing cannot continue as the configured taxonomy exception count of {_taxonomyExceptionThreshold} has been reached.", ex);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("Fatal Error:" + ex.Message);

                    Exception inner = ex.InnerException;

                    while (inner != null)
                    {
                        _logger.LogCritical("Inner Exception: " + inner.Message);
                        inner = inner.InnerException;
                    }

                    _logger.LogCritical(ex.StackTrace);
                    throw;
                }
            }
            if (!token.IsCancellationRequested)
            {
                _logger.LogInformation(IAID_QUEUE_COMPLETE_MSG);
            }
            else
            {
                string cancelMessage =  "Full reindex single batch processing operation cancelled.  Plesae check the logs for further errors.";
                throw new OperationCanceledException(cancelMessage, token);
            }
        }

        private  void SubmitForCategorisation(string[][] batches)
        {
            int totalCount = batches.SelectMany(a => a).Count();

            DateTime queryStart = DateTime.Now;

            _logger.LogInformation($"Submitting a batch of {totalCount} for categorisation.");

            var tasks = new List<Task<IDictionary<string, List<CategorisationResult>>>>();

            foreach(string[] batch in batches)
            {
                Task<IDictionary<string, List<CategorisationResult>>> task = Task.Run(() => _categoriserService.CategoriseMultiple(batch));
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            DateTime queryEnd = DateTime.Now;
            TimeSpan queryTime = queryEnd - queryStart;
            
            _logger.LogInformation($" Finished categorising {totalCount} assets. Operation took {Math.Round(queryTime.TotalSeconds, 3)} seconds.");

            var results = tasks.SelectMany(t => t.Result);
            _parent.IncrementCategorisationCount(results.Count());

            if (_logIndividualCategorisationResults)
            {
                
                foreach (var result in results)
                {
                    LogResults(result); 
                }
            }
        }

        private void LogResults(KeyValuePair<string, List<CategorisationResult>> resultSet)
        {
                int numberOfCategoriesMatched = resultSet.Value.Count;
                _logger.LogInformation($"  - Categorised {resultSet.Key}, {numberOfCategoriesMatched} " + (numberOfCategoriesMatched == 1 ? "category" : "categories") + $" found: " + String.Join(';', resultSet.Value));
        }
    }
}
