using AutoMapper;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public class AbstractOpenSearchRespository<T> where T : class
    {
        protected readonly IConnectOpenSearch<T> _openSearchConnection;
        protected readonly IMapper _mapper;
        public AbstractOpenSearchRespository(IConnectOpenSearch<T> openSearchConnection, IMapper mapper)
        {
            _openSearchConnection = openSearchConnection;
            _mapper = mapper;
        }
    }
}
