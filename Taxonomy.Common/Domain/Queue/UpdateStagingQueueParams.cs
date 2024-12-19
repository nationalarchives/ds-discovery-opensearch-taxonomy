namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class UpdateStagingQueueParams
    {
        public AmazonSqsParams AmazonSqsParams { get; set;}

        public int WorkerCount { get; set; } = 1;

        public int MaxErrors { get; set; } = 1;

        public int BatchSize { get; set; }

        public bool EnableVerboseLogging { get; set; }

        public bool PostUpdates { get; set; }

        public int SendIntervalMilliseconds { get; set; }
    }
}
