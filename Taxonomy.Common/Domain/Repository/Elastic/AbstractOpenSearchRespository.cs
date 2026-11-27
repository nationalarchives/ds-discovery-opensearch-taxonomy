using AutoMapper;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public class AbstractOpenSearchRespository<T> where T : class
    {
        protected readonly IConnectOpenSearch<T> _openSearchConnection;
        public AbstractOpenSearchRespository(IConnectOpenSearch<T> openSearchConnection)
        {
            _openSearchConnection = openSearchConnection;
        }
    }
}
