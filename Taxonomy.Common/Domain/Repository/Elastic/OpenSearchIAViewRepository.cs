using AutoMapper;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.Lucene;
using NationalArchives.Taxonomy.Common.Helpers;
using OpenSearch.Client;

//using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public class OpenSearchIAViewRepository : AbstractOpenSearchRespository<OpenSearchRecordAssetView>, IIAViewRepository
    {
        private LuceneHelperTools _luceneHelperTools;

        public OpenSearchIAViewRepository(IConnectOpenSearch<OpenSearchRecordAssetView> openSearchConnection, LuceneHelperTools luceneHelperTools, IMapper mapper) : base(openSearchConnection, mapper)
        {
            _luceneHelperTools = luceneHelperTools;
        }

        public async Task<InformationAssetView> SearchDocByDocReference(string docReference)
        {
            try
            {
                // N.B> the class name has to match the _type field in Elastic  e.g. _type=recordassetview
                // i.e. the URL is  GET: /discovery_records_dev/recordassetview/C1505}
                // So passing ElasticSearchResultAssetView will bring back a 404 even though though its a base type
                //IGetResponse<RecordAssetView> searchResponse =  await elasticClient.GetAsync<RecordAssetView>(docReference);
                
                IGetResponse<OpenSearchRecordAssetView> response = await _openSearchConnection.GetAsync(docReference);
                if(!response.IsValid)
                {
                    throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_INVALID_RESPONSE, $"Error retrieving document id {docReference} from Open Search", response.OriginalException);
                }

                OpenSearchRecordAssetView searchResult = response.Source;

                //Alternative using ISearchResponse instead of IGetResponse.  NOt sure which is preferred
                // - Get is detailed at https://www.elastic.co/guide/en/elasticsearch/client/net-api/1.x/get.html
                // but it seems to be missing from the V6 docmentation though doesn't seem to be deprecated.
                // One advantage is with ISearchResponse we can use base type ElasticSearchResultAssetView.

                // InformationAssetView == BO
                var infoAsset = _mapper.Map<InformationAssetView>(searchResult);


                return infoAsset;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                throw;
            }
        }

        public async Task<IList<InformationAssetView>> SearchDocByMultipleDocReferences(string[] docReferences)
        {

            List<InformationAssetView> informationAssets = new List<InformationAssetView>();

            try
            {
                MultiGetResponse response = await _openSearchConnection.MultiGetAsync(docReferences);

                if (!response.IsValid)
                {
                    throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_INVALID_RESPONSE, $"Error retrieving mutiple document request from Elasic Search.  The IAIDs submitted were: {String.Join(";", docReferences)}", response.OriginalException);
                }

                var results = response.GetMany<OpenSearchRecordAssetView>(docReferences);

                foreach (var result in results)
                {
                    OpenSearchRecordAssetView searchResult = result.Source;
                    var infoAsset = _mapper.Map<InformationAssetView>(searchResult);
                    informationAssets.Add(infoAsset);
                }

                return informationAssets;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                throw;
            }
        }

        public IList<CategorisationResult> FindRelevantCategoriesForDocument(InformationAssetView iaView, IList<Category> sourceCategories, bool includeScores = false)
        {

            //In the Java/Lucene version, this loads a single IAView into memory and then throws each of the source categories at it to see if there's a hit
            //  If so, it adds the category to the return list.  So effectively then filtering the input list of categories to see which match.
            // This usies Lucene library classes e.g. RAMDirectory and IndexSearcher  to add in memory mapping and run queries.  
            // Currently using multi match in Elastic Search.  But there may be a better way e.g. filters, or .NET port of Lucene
            // library where we could do in memory.

            // TODO: Is "includeScores" the best strategy, or are we better off always getting the results without asking for scores first, and then
            // running the hits again to get the scores?  In memory version seems only slightly faster with scores

            try
            {

                OpenSearchRecordAssetView esAsset = _mapper.Map<InformationAssetView, OpenSearchRecordAssetView>(iaView);
                IndexResponse response = _openSearchConnection.IndexDocument(esAsset, useInmemoryIndex: true);

                //Race condition in ES?
                //System.Threading.Thread.Sleep(1000);
                // TODO: We can use either an IDs query or term query to specify the ID to search on.  Can look at which gives beeter performance, so far not
                // much in it really.

                //Basic term query to search on ID - to be combined with categry queries into multi search request.
                var base_term_query = new TermQuery
                {
                    IsVerbatim = true,
                    Field = "_id",
                    Value = iaView.DocReference,
                };

                // Ids query -  - to be combined with category queries into multi search request.
                var idsQuery = new IdsQuery
                {
                    Values = new Id[] { iaView.DocReference }
                };

                //return matchedCategoriesInMemory;
                // TODO: Async ?
                IList<CategorisationResult> matchedCategories = _openSearchConnection.CategoryMultiSearch(base_term_query, sourceCategories, true, includeScores, 50);

                //TODO: Possibly mange within the connection itself.
                _openSearchConnection.DeleteDocumentFromIndex(iaView.DocReference, true);

                return matchedCategories;

            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                throw;
            }
        }

        public async Task<PaginatedList<InformationAssetViewWithScore>> PerformSearch(String query, Double minScore, int limit, int offset, HeldByCode heldByCode, bool useDefaultTaxonomyField)
        {
            try
            {
                var openSearchParamsBuilder = new OpenSearchParamsBuilder();
                OpenSearchParameters searchParams = openSearchParamsBuilder.GetOpenSearchParameters(query: query, heldByCode: heldByCode, pageSize: limit, pagingOffset: offset);

                var fieldList = new List<string>();

                if (!useDefaultTaxonomyField)
                {
                    fieldList.AddRange(_luceneHelperTools.QueryFields);
                    
                }
                else
                {
                    fieldList.Add(_luceneHelperTools.DefaultTaxonomyField);
                }
                
                searchParams.SearchFields = fieldList;

                ISearchResponse<OpenSearchRecordAssetView> searchResponse = await _openSearchConnection.SearchAsync(searchParams);

                Debug.Print(searchResponse.GetType().Name);

                var paginatedListFactory = new IAListFactory<OpenSearchRecordAssetView, InformationAssetViewWithScore>(searchResponse, _mapper);
                var paginatedList = paginatedListFactory.CreatePaginatedList(limit: limit, offset: offset, minScore: minScore);

                return paginatedList;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                throw;
            }
        }

        public InformationAssetScrollList BrowseAllDocReferences(OpenSearchAssetBrowseParams browseParams,  string scrollId = null)
        {

            try
            {
                if (String.IsNullOrWhiteSpace(scrollId))
                {
                    var openSearchParamsBuilder = new OpenSearchParamsBuilder();
                    OpenSearchParameters searchParams = openSearchParamsBuilder.GetSearchParametersForScroll(browseParams);

                    // Get the first set of results which includes the sccroll ID to use in future requests.
                    Task<ISearchResponse<OpenSearchRecordAssetView>> assetFetch = _openSearchConnection.SearchAsync(searchParams);
                    var awaiter = assetFetch.GetAwaiter();
                    var searchResponse = awaiter.GetResult();

                    if(assetFetch.IsFaulted)
                    {
                        throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_SCROLL_EXCEPTION, "Unable to fetch list of asset IDs on initial scroll request.", assetFetch.Exception.Flatten());
                    }

                    if (String.IsNullOrEmpty(searchResponse.ScrollId))
                    {
                        throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_SCROLL_EXCEPTION, "Unable to retrieve scroll ID for paging information assets.");
                    }

                    return  new InformationAssetScrollList(searchResponse.ScrollId, searchResponse.Hits.Select(h => h.Id).ToList());
                }
                else  // existing scroll request
                {
                    var scrollResponse = _openSearchConnection.ScrollAsync(browseParams.PageSize, scrollId);
                    if (scrollResponse.Result.Hits.Any())
                    {
                        return new InformationAssetScrollList (scrollId, scrollResponse.Result.Hits.Select(h => h.Id).ToList());
                    }
                    else
                    {
                        //TODO: Async?
                        var response = _openSearchConnection.ClearScroll(scrollId).Result;
                        if(!response.IsValid)
                        {
                            //throw new TaxonomyException(TaxonomyErrorType.ELASTIC_SCROLL_EXCEPTION, "Error clearing Information Asset Scroll", response.OriginalException);
                        }
                        return new InformationAssetScrollList(scrollId, new List<string>());
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}


