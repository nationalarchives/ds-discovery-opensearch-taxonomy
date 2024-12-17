namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsParams
    {
        public string QueueUrl { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        public string Region {get; set;}
        public string RoleArn { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string SessionToken { get; set; }
        public int MaxSize { get; set; }
        public int WaitMilliseconds { get; set; }
    }
}
