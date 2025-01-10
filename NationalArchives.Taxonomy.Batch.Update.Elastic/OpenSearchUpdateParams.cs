using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Batch
{
    internal sealed class OpenSearchUpdateParams
    {
        public int  BulkUpdateBatchSize { get; set; }

        public int QueueFetchSleepTime { get; set; }

        public int SearchDatabaseUpdateInterval { get; set; }

        public int MaxInternalQueueSize { get; set; }
    }
}
