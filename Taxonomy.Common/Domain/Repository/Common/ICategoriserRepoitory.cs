using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Common
{
    public interface ICategoriserRepository
    {
        IList<CategorisationResult> FindRelevantCategoriesForDocument(InformationAssetView iaView, IEnumerable<Category> sourceCategories, bool includeScores = false);

        IDictionary<string, List<CategorisationResult>> FindRelevantCategoriesForDocuments(InformationAssetView[] iaViews, IEnumerable<Category> sourceCategories, bool includeScores = false);
    }
}
