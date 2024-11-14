using NationalArchives.Taxonomy.Common.Domain;
using NationalArchives.Taxonomy.Common.DataObjects.Elastic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;

namespace NationalArchives.Taxonomy.Common.Service
{
    public interface IInformationAssetViewService
    {
        Task<PaginatedList<InformationAssetViewWithScore>> PerformSearch(String query, Double score, int limit, int offset, string strHeldBy = "TNA ");

        InformationAssetScrollList BrowseAllDocReferences(ElasticAssetBrowseParams browseParams, string scrollId = null);
    }
}
