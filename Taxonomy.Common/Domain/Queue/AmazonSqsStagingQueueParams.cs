using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsStagingQueueParams
    {
        public string QueueUrl { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        public string Region {get; set;}
        public string RoleArn { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string SessionToken { get; set; }
        public int MaxSize { get; set; }
        public int WorkerCount { get; set; } = 1;
        public int MaxErrors { get; set; } = 1;
        public int BatchSize { get; set; }
        public bool EnableVerboseLogging { get; set; }
        public bool PostUpdates { get; set; }
    }
}
