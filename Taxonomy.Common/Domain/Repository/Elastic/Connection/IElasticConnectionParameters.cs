using System;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    public interface IElasticConnectionParameters
    {
        string Host { get; set; }
        string IndexDatabase { get; set; }
        int Port { get; set; }
        string Scheme { get; set; }
        Uri Uri { get; }
    }
}