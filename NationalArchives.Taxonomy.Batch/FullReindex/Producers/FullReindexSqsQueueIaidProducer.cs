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

        public int TotalIdentifiersFetched => throw new NotImplementedException();

        public int CurrentQueueSize => throw new NotImplementedException();

        public Task InitAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task HandleTextMessage(IList<string> iaids)
        {
            throw new NotImplementedException();
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
