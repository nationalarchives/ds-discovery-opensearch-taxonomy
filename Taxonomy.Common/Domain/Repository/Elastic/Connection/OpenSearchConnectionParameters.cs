using System;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public abstract class OpenSearchConnectionParameters : IOpenSearchConnectionParameters
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

        public OpenSearchAwsParams OpenSearchAwsParams { get; set; }

    }

    public sealed class DiscoveryOpenSearchConnectionParameters : OpenSearchConnectionParameters
    {
       
    }

    public sealed class CategoryDataOpenSearchConnectionParameters : OpenSearchConnectionParameters
    {

    }

    public sealed class CategoriserOpenSearchConnectionParameters : OpenSearchConnectionParameters
    {

    }

    public sealed class UpdateOpenSearchConnectionParameters : OpenSearchConnectionParameters
    {

    }

    public sealed class OpenSearchAwsParams 
    {
        [Obsolete]
        public bool UseAwsConnection { get; set; }
        public OpenSearchConnectionMode OpenSearchConnectionMode { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public string RoleArn { get; set; }
        public string SessionToken { get; set; }
    }

    public enum OpenSearchConnectionMode
    {
        Agnostic,
        AwsBasic,
        EC2,
        None
    }
}