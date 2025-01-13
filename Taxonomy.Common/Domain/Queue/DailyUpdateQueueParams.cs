using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class DailyUpdateQueueParams
    {
        public AmazonSqsParams AmazonSqsParams { get; set; }

        [Obsolete]
        public int WorkerCount { get; set; } = 1;

        [Obsolete]
        public int MaxErrors { get; set; } = 1;

        [Obsolete]
        public int BatchSize { get; set; }

        [Obsolete]
        public bool EnableVerboseLogging { get; set; }

        [Obsolete]
        public bool PostUpdates { get; set; }
    }
}
