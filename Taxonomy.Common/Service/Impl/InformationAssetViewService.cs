using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Service
{
    public class InformationAssetViewService : IInformationAssetViewService
    {
        private readonly IIAViewRepository _iaViewRepository; 
        private bool _useDfaultTaxonomyField;

        //TODO Replace IElasticConnectionInfo with Repository injection.
        public InformationAssetViewService(IIAViewRepository iAViewRepository, bool useDefaultTaxonomyFieldForApiSearch =false)     
        {
            _iaViewRepository = iAViewRepository;
            _useDfaultTaxonomyField = useDefaultTaxonomyFieldForApiSearch;
        }

        public InformationAssetScrollList BrowseAllDocReferences(OpenSearchAssetBrowseParams browseParams,  string scrollId)
        {
            try
            {
                var results = _iaViewRepository.BrowseAllDocReferences(browseParams,  scrollCursor: scrollId);
                return results;
            }
            catch (Exception e)
            {
                throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_INVALID_RESPONSE, "Error retrieving information asset IDs from Elastic Search", e);
            }
        }

        //TODO: determine return type and implement IInformationAssetViewService.
        public Task<PaginatedList<InformationAssetViewWithScore>> PerformSearch(String query, Double minScore, int limit, int offset, string strHeldBy = "TNA")
        {
            HeldByCode heldByCode = (HeldByCode)Enum.Parse(typeof(HeldByCode), strHeldBy);

            if(String.IsNullOrWhiteSpace(query))
            {
                throw new TaxonomyException("No query supplied for search request!");
            }

            try
            {
                var paginatedList = PerformSearchAsync(query, minScore, limit, offset, heldByCode);
                return paginatedList;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                throw;
            }

            async Task<PaginatedList<InformationAssetViewWithScore>> PerformSearchAsync(String query1, Double minScore1, int limit1, int offset1, HeldByCode heldByCode1)
            {
                var paginatedList = await _iaViewRepository.PerformSearch(query, minScore, limit, offset, heldByCode1, _useDfaultTaxonomyField);
                return paginatedList;
            }
        }
    }
}
