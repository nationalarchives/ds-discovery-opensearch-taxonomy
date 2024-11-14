using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Batch
{
    internal sealed class OpenSearchUpdateParams
    {
        public uint  BulkUpdateBatchSize { get; set; }

        public uint QueueFetchSleepTime { get; set; }
    }
}
