using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    internal class ElasticSearchParamsBuilder
    {
        public ElasticSearchParameters GetElasticSearchParametersForScroll(ElasticAssetBrowseParams elasticAssetBrowseParams)
        {
            ElasticSearchParameters searchParams = new ElasticSearchParameters()
            {
                Query = string.Empty,  // Sends a match all query
                PageSize = elasticAssetBrowseParams.PageSize,
                Scroll = elasticAssetBrowseParams.ScrollTimeout,
                IncludeSource = false, // Don't include the actual doc i.e. Elastic src as we only need the ID which we can get from the hit info.
                HeldByCode = elasticAssetBrowseParams.HeldByCode
            };

            if (elasticAssetBrowseParams.HeldByCode != HeldByCode.ALL)
            {
                searchParams.FilterQueries.Add(new KeyValuePair<string, IEnumerable<object>>(ElasticFieldConstants.ES_HELD_BY_CODE, new[] { elasticAssetBrowseParams.HeldByCode.ToString() }));
            }

            return searchParams;
        }

        public ElasticSearchParameters GetElasticSearchParameters(int pagingOffset, int pageSize)
        {
            ElasticSearchParameters searchParams = new ElasticSearchParameters() { PagingOffset = pagingOffset, PageSize = pageSize };
            return searchParams;
        }

        public ElasticSearchParameters GetElasticSearchParameters(string query, HeldByCode heldByCode, int pagingOffset, int pageSize)
        {
            ElasticSearchParameters searchParams = new ElasticSearchParameters()
            {
                Query = query,
                PagingOffset = pagingOffset,
                PageSize = pageSize,
                HeldByCode = heldByCode
            };

            if (searchParams.HeldByCode != HeldByCode.ALL)
            {
                searchParams.FilterQueries.Add(new KeyValuePair<string, IEnumerable<object>>(ElasticFieldConstants.ES_HELD_BY_CODE, new[] { searchParams.HeldByCode.ToString() }));
            }

            return searchParams;
        }
    }
}
