using System;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    public abstract class ElasticConnectionParameters : IElasticConnectionParameters
    {
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        // This was used during development when looking at using an in memory Elastic Search index.
        // We now use Lucene dircetly in memory as this avoids the overhead of an HTTP call and is much faster.
        [Obsolete]
        public virtual string InMemoryIndexName { get; set; }
        public string IndexDatabase { get; set; }
        public Uri Uri
        {
            get 
            {
                UriBuilder uriBuilder = new UriBuilder();
                uriBuilder.Scheme = Scheme;
                uriBuilder.Host = Host;
                uriBuilder.Port = Port;
                Uri uri = uriBuilder.Uri;
                return uri;
            }
        }

        public int RequestTimeout { get; set; }

        public ElasticAwsParams ElasticAwsParams { get; set; }

    }

    public sealed class DiscoverySearchElasticConnectionParameters : ElasticConnectionParameters
    {
       
    }

    public sealed class CategoryDataElasticConnectionParameters : ElasticConnectionParameters
    {

    }

    public sealed class CategoriserElasticConnectionParameters : ElasticConnectionParameters
    {

    }

    public sealed class UpdateElasticConnectionParameters : ElasticConnectionParameters
    {

    }

    public sealed class ElasticAwsParams 
    {
        public bool UseAwsConnection { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public string RoleArn { get; set; }
    }
}