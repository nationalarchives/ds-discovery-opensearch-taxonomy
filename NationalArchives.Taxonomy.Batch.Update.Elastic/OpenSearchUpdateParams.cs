using NationalArchives.Taxonomy.Common.Domain.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Batch
{
    internal sealed class OpenSearchUpdateParams
    {
        public AmazonSqsParams AmazonSqsParams { get; set; }

        public int  BulkUpdateBatchSize { get; set; }

        public int QueueFetchSleepTime { get; set; }

        public int SearchDatabaseUpdateInterval { get; set; }

        public int MaxInternalQueueSize { get; set; }
        public int WaitMilliseconds { get; set; }
    }
}
