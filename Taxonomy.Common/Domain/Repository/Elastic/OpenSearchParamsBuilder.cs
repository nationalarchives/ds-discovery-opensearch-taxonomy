using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    internal class OpenSearchParamsBuilder
    {
        public OpenSearchParameters GetSearchParametersForScroll(OpenSearchAssetBrowseParams openSearchAssetBrowseParams)
        {
            OpenSearchParameters searchParams = new OpenSearchParameters()
            {
                Query = string.Empty,  // Sends a match all query
                PageSize = openSearchAssetBrowseParams.PageSize,
                Scroll = openSearchAssetBrowseParams.ScrollTimeout,
                IncludeSource = false, // Don't include the actual doc i.e. src as we only need the ID which we can get from the hit info.
                HeldByCode = openSearchAssetBrowseParams.HeldByCode
            };

            if (openSearchAssetBrowseParams.HeldByCode != HeldByCode.ALL)
            {
                searchParams.FilterQueries.Add(new KeyValuePair<string, IEnumerable<object>>(OpenSearchFieldConstants.ES_HELD_BY_CODE, new[] { openSearchAssetBrowseParams.HeldByCode.ToString() }));
            }

            return searchParams;
        }

        public OpenSearchParameters GetOpenSearchParameters(int pagingOffset, int pageSize)
        {
            OpenSearchParameters searchParams = new OpenSearchParameters() { PagingOffset = pagingOffset, PageSize = pageSize };
            return searchParams;
        }

        public OpenSearchParameters GetOpenSearchParameters(string query, HeldByCode heldByCode, int pagingOffset, int pageSize)
        {
            OpenSearchParameters searchParams = new OpenSearchParameters()
            {
                Query = query,
                PagingOffset = pagingOffset,
                PageSize = pageSize,
                HeldByCode = heldByCode
            };

            if (searchParams.HeldByCode != HeldByCode.ALL)
            {
                searchParams.FilterQueries.Add(new KeyValuePair<string, IEnumerable<object>>(OpenSearchFieldConstants.ES_HELD_BY_CODE, new[] { searchParams.HeldByCode.ToString() }));
            }

            return searchParams;
        }
    }
}
