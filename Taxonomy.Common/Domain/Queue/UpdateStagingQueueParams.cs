using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class UpdateStagingQueueParams
    {
        public string Uri { get; set; }
        public string QueueName { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public int MaxSize { get; set; }

        public int WorkerCount { get; set; } = 1;

        public int MaxErrors { get; set; } = 1;

        public int BatchSize { get; set; }

        public bool EnableVerboseLogging { get; set; }

        public bool PostUpdates { get; set; }
    }
}
