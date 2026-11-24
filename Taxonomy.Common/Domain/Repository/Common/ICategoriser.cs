using NationalArchives.Taxonomy.Common.BusinessObjects;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Common
{
    public interface ICategoriser
    {
        IList<CategorisationResult> FindRelevantCategoriesForDocument(InformationAssetView iaView, IEnumerable<Category> sourceCategories, bool includeScores = false);

        IDictionary<string, List<CategorisationResult>> FindRelevantCategoriesForDocuments(InformationAssetView[] iaViews, IEnumerable<Category> sourceCategories, bool includeScores = false);
    }

    public interface IRefreshableCategoriser
    {
        void Refresh();
    }
}
