using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public interface IIAViewRepository
    {
        IList<CategorisationResult> FindRelevantCategoriesForDocument(InformationAssetView iaView, IList<Category> sourceCategories, bool includeScores = false);
        Task<PaginatedList<InformationAssetViewWithScore>> PerformSearch(string query, double minScore, int limit, int offset, HeldByCode heldByCode, bool useDefaultTaxonomyField = false);
        Task<InformationAssetView> SearchDocByDocReference(string docReference);

        Task<IList<InformationAssetView>> SearchDocByMultipleDocReferences(string[] docReference);
        InformationAssetScrollList BrowseAllDocReferences(OpenSearchAssetBrowseParams browseParams, string scrollCursor);
    }
}
