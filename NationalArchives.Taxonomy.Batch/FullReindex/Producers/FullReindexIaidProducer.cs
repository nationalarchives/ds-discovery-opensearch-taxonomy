using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.FullReindex.Queues;
using NationalArchives.Taxonomy.Batch.Service;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.FullReindex.Producers
{
    internal class FullReindexIaidProducer : IIAIDProducer
    {

        private const int SCROLL_TIMEOUT = 60000; //  1 min.  probably > enough..

        private FullReIndexIaidPcQueue<string> _pcQueue;
        private readonly IInformationAssetViewService _iaViewService;
        private readonly ILogger<FullReindexService> _logger;
        private OpenSearchAssetBrowseParams _openSearchAssetBrowseParams;

        internal EventHandler<MessageProcessingEventArgs> ProcessingCompleted;
        internal EventHandler<MessageProcessingEventArgs> FatalException;

        private int _totalCount;

        public FullReindexIaidProducer(FullReIndexIaidPcQueue<string> pcQueue, IInformationAssetViewService iaViewService, OpenSearchAssetBrowseParams openSearchAssetFetchParams,ILogger<FullReindexService> logger)
        {
            _pcQueue = pcQueue;
            _iaViewService = iaViewService;
            _logger = logger;
            _openSearchAssetBrowseParams = openSearchAssetFetchParams;
        }

        public async Task InitAsync(CancellationToken token)
        {
            try
            {

                Task task = Task.Run(() =>
                    {

                    DateTimeOffset startTime = DateTimeOffset.Now;
                    DateTimeOffset currentTime;

                        int totalScrollResults = 0;

                    InformationAssetScrollList informationAssetsList = _iaViewService.BrowseAllDocReferences(browseParams: _openSearchAssetBrowseParams, scrollId: null);

                    string scrollId = informationAssetsList.ScrollId;

                    if (String.IsNullOrEmpty(scrollId))
                    {
                        throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_SCROLL_EXCEPTION, "Error scrolling IAIDS in Open Search.  Could not retrieve Open Search Scroll ID");
                    }

                        if (informationAssetsList.ScrollResults.Count == 0)
                        {
                            throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_SCROLL_EXCEPTION, "No results received on initial scroll request.");
                        }
                        else
                        {
                            totalScrollResults += informationAssetsList.ScrollResults.Count;
                            // Add the first batch of results to the queue.
                            AddToQueue(informationAssetsList.ScrollResults);

                            int scrollCount = 1;

                            _logger.LogInformation($"Scroll iteration {scrollCount}.  Results this iteration: {informationAssetsList.ScrollResults.Count}.  Total results so far: {totalScrollResults}");
                            if (_openSearchAssetBrowseParams.LogFetchedAssetIds)
                            {
                                LogScrollResults(informationAssetsList, scrollCount);
                            }



                            // Keep fetching results on the scroll cursor until nothing comes back..
                            do
                            {
                                scrollCount++;

                                informationAssetsList = _iaViewService.BrowseAllDocReferences(browseParams: _openSearchAssetBrowseParams, scrollId: scrollId);

                                if (informationAssetsList.ScrollResults?.Count > 0)
                                {
                                    totalScrollResults += informationAssetsList.ScrollResults.Count;

                                    if (String.IsNullOrEmpty(informationAssetsList.ScrollId))
                                    {
                                        throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_SCROLL_EXCEPTION, "error during scrolling IAIDs from Open Search - could not retrieve scroll ID.");
                                    }
                                    else
                                    {
                                        //Scroll ID can change during the scrolls - see https://www.elastic.co/guide/en/elasticsearch/reference/6.8/search-request-scroll.html
                                        scrollId = informationAssetsList.ScrollId;
                                    }

                                    _logger.LogInformation($"Scroll iteration {scrollCount}.  Results this iteration: {informationAssetsList.ScrollResults.Count}.  Total results so far: {totalScrollResults}");
                                    if (_openSearchAssetBrowseParams.LogFetchedAssetIds)
                                    {
                                        LogScrollResults(informationAssetsList, scrollCount);
                                    }
                                    
                                }
                                else
                                {
                                    StringBuilder sb = new StringBuilder($"No asset identifier results received from Open Search scroll cursor on scroll count {scrollCount}.  Total results so far {totalScrollResults}. ");
                                    if (token.IsCancellationRequested)
                                    {
                                        sb.Append("Cancellation of Open Search scroll was requested.");
                                    }
                                    else
                                    {
                                        sb.Append("Cancellation of Open Search scroll was not requested.");
                                    }
                                    _logger.LogInformation(sb.ToString());
                                }

                                AddToQueue(informationAssetsList.ScrollResults);

                                if (_pcQueue.Count % 100000 == 0)
                                {
                                    currentTime = DateTimeOffset.Now;
                                    TimeSpan elaspsed = currentTime - startTime;
                                    _logger.LogInformation($"Running for {Math.Round(elaspsed.TotalMinutes, 2)} minutes.");

                                    Debug.Print($"{_pcQueue.Count} documents in the iaid queue.\n");
                                    _logger.LogInformation($"{_pcQueue.Count} documents in the iaid queue.\n");
                                    _logger.LogInformation($"Current memory usage is {Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024} MB.");

                                }
                            }
                            while (!token.IsCancellationRequested && informationAssetsList.ScrollResults.Count > 0);

                            _pcQueue.CompleteAdding();
                            // Takes c. 65 minutes to scroll through 10 million docs.
                            // And about 1 GB per 10 million IAID strings in memory.

                            if (!token.IsCancellationRequested)
                            {
                                string completionMessage =  "Fetch of IAIDs from Open Search to processing queue completed.";
                                _logger.LogInformation(completionMessage); 
                            }
                            else
                            {
                                string cancelMessage = "Fetch of source information assets from Open Search for categorisation was cancelled by the caller.";
                                throw new OperationCanceledException(cancelMessage, token);
                            }
                        }
                    }, token
                 );

                await task;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        private void LogScrollResults(InformationAssetScrollList informationAssetsList, int scrollCount)
        {
            _logger.LogInformation($"Fetched {informationAssetsList.ScrollResults.Count} asset IDs on scroll {scrollCount} {String.Join(';', informationAssetsList.ScrollResults)}");
        }

        private void AddToQueue(IReadOnlyCollection<string> scrollResults)
        {
            foreach (string s in scrollResults)
            {
                bool success = _pcQueue.Enqueue(s);
                if(!success)
                {
                    throw new TaxonomyException( TaxonomyErrorType.IAID_QUEUE_ADD_FAILURE, "Error adding an item to the staging queue for full reindex.  Check the configured queue size.");
                }
            }
            _totalCount += scrollResults.Count;
        }

        public int TotalIdentifiersFetched { get => _totalCount; }
        public int CurrentQueueSize { get => _pcQueue.Count; }
    }
}
