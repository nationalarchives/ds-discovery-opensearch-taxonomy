using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.FullReindex.Queues;
using NationalArchives.Taxonomy.Batch.Service;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.FullReindex.Producers
{
    internal class FullReindexSqsQueueIaidProducer : AmazonSqsConsumerBase, IIAIDProducer
    {
        private readonly FullReIndexIaidPcQueue<string> _pcQueue;
        private readonly IInformationAssetViewService _iaViewService;
        private readonly OpenSearchAssetBrowseParams _openSearchAssetBrowseParams;

        private volatile int _totalCount;

        public FullReindexSqsQueueIaidProducer(FullReindexQueueParams qParams, FullReIndexIaidPcQueue<string> pcQueue, IInformationAssetViewService iaViewService, 
            OpenSearchAssetBrowseParams openSearchAssetFetchParams, ILogger<FullReindexService> logger) : base(qParams.AmazonSqsParams, logger)
        {
            _pcQueue = pcQueue;
            _iaViewService = iaViewService;
            _openSearchAssetBrowseParams = openSearchAssetFetchParams;
        }

        public int TotalIdentifiersFetched => _totalCount;

        public int CurrentQueueSize => _pcQueue.Count;
        public async Task InitAsync(CancellationToken token)
        {
            try
            {
                await Task.Run(() => base.Init(token));
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        protected override async Task HandleTextMessage(IList<string> iaids)
        {
            try
            {
                //TODO: Unlike the database cursor based fetch, There is no way here as yet to filter out non-TNA records, if the input queue includes these.
                _totalCount += iaids.Count;
                // Add the first batch of results to the queue.
                AddToQueue(iaids);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Fatal Error: {ex.Message}");
                if (!_tcsInit.Task.IsFaulted)
                {
                    _tcsInit.SetException(ex); 
                }
                throw;
            }
;
        }

        private void AddToQueue(IList<string> scrollResults)
        {
            foreach (string s in scrollResults)
            {
                bool success = _pcQueue.Enqueue(s);
                if (!success)
                {
                    throw new TaxonomyException(TaxonomyErrorType.IAID_QUEUE_ADD_FAILURE, "Error adding an item to the staging queue for full reindex.  Check the configured queue size.");
                }
            }
            _totalCount += scrollResults.Count;
        }
    }
}
