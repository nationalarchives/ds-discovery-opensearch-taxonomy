using NationalArchives.Taxonomy.Common.BusinessObjects;
using OpenSearch.Client;
//using Nest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public interface IConnectOpenSearch<T> where T : class
    {
        ISearchResponse<T> Search(OpenSearchParameters searchCommand);

        Task<ISearchResponse<T>> SearchAsync(OpenSearchParameters searchCommand);

        IGetResponse<T> Get(string id);

        Task<IGetResponse<T>> GetAsync(string id);

        Task<MultiGetResponse> MultiGetAsync(string[] ids);

        IndexResponse IndexDocument(T documentToIndex, bool useInmemoryIndex);

        IList<CategorisationResult> CategoryMultiSearch(QueryBase baseOrIdsQuery, IList<Category> sourceCategories, bool useInMemoryIndex, bool includeScores, int maxConcurrentQueries);

        long Count(OpenSearchParameters countCommand);

        Task<ISearchResponse<T>> ScrollAsync(int scrollTimeout, string scrollId);

        Task<ClearScrollResponse> ClearScroll(string scrollId);

        void DeleteDocumentFromIndex(string documentId, bool useInMemoryIndex);
    }
}