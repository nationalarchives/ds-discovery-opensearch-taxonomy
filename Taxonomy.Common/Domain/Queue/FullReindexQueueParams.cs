using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class FullReindexQueueParams
    {
        public AmazonSqsParams AmazonSqsParams { get; set; }

        public int WorkerCount { get; set; } = 1;

        public int MaxErrors { get; set; } = 1;

        public int MaxSize { get; set; }

        public string IaidSource { get; set; }
    }
}
