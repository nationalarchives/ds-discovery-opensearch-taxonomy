using System;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public interface IOpenSearchConnectionParameters
    {
        string Host { get; set; }
        string IndexDatabase { get; set; }
        int Port { get; set; }
        string Scheme { get; set; }
        Uri Uri { get; }
    }
}