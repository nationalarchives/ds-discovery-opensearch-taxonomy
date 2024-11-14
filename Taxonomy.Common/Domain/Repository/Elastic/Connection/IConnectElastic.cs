using NationalArchives.Taxonomy.Common.BusinessObjects;
using Nest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    public interface IConnectElastic<T> where T : class
    {
        ISearchResponse<T> Search(ElasticSearchParameters searchCommand);

        Task<ISearchResponse<T>> SearchAsync(ElasticSearchParameters searchCommand);

        IGetResponse<T> Get(string id);

        Task<IGetResponse<T>> GetAsync(string id);

        Task<IMultiGetResponse> MultiGetAsync(string[] ids);

        IIndexResponse IndexDocument(T documentToIndex, bool useInmemoryIndex);

        IList<CategorisationResult> CategoryMultiSearch(QueryBase baseOrIdsQuery, IList<Category> sourceCategories, bool useInMemoryIndex, bool includeScores, int maxConcurrentQueries);

        long Count(ElasticSearchParameters countCommand);

        Task<ISearchResponse<T>> ScrollAsync(int scrollTimeout, string scrollId);

        Task<IClearScrollResponse> ClearScroll(string scrollId);

        void DeleteDocumentFromIndex(string documentId, bool useInMemoryIndex);
    }
}