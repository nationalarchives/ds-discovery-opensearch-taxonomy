using NationalArchives.Taxonomy.Common.BusinessObjects;
using OpenSearch.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public class OpenSearchConnection<T> : IConnectOpenSearch<T> where T : class
    {
        private IOpenSearchClient _openSearchClient;
        private IOpenSearchClient _openSearchClientInMemory; //TODO : Possibly use a separate connection
        private ISearchRequest _searchRequest;

        private OpenSearchConnectionParameters _parameters;

        private string _inMemoryIndexName;

        private const string HELD_BY_CODE = "HELD_BY_CODE";
        private const string RESPSITORY = "RESPSITORY";

        public OpenSearchConnection(OpenSearchConnectionParameters openSearchConnectionParameters)
        {
            _parameters = openSearchConnectionParameters;
            //ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
            using (ConnectionSettings connectionSettings = ConnectionSettingsProvider.GetConnectionSettings(_parameters))
            {
                //connectionSettings.DisableAutomaticProxyDetection(true);
                _openSearchClient = new OpenSearchClient(connectionSettings);
            }
        }

        public long Count(OpenSearchParameters searchParams)
        {
            var countRequest = new CountRequest(_parameters.IndexDatabase)
            {
                Query = SetupSearchRequest(searchParams)
            };

            var countResponse = _openSearchClient.Count(countRequest);

            if (!countResponse.IsValid)
            {
                throw new WebException($@"{countResponse.ServerError}
                                        {Environment.NewLine}Debug information: {countResponse.DebugInformation}
                                        {Environment.NewLine}Reasons: [First] -> {countResponse.ServerError?.Error?.RootCause?.First().Reason}
                [Last] -> {countResponse.ServerError?.Error?.RootCause?.Last().Reason}", countResponse.OriginalException);
            }

            return countResponse.Count;
        }


        public ISearchResponse<T> Search(OpenSearchParameters searchParams)
        {
            _searchRequest = new SearchRequest(_parameters.IndexDatabase)
            {
                Query = SetupSearchRequest(searchParams),
                Aggregations = SetupFacets(searchParams.FacetFields),
                Sort = SetSortOrder(searchParams.Sort)
            };

            var searchResponse =  _openSearchClient.Search<T>(_searchRequest);

            RaiseExceptionIfResponseIsInvalid(searchResponse);

            return searchResponse;
        }

        public async Task<ISearchResponse<T>> SearchAsync(OpenSearchParameters searchParams)
        {
            _searchRequest = new SearchRequest(_parameters.IndexDatabase)
            {   
                Query = SetupSearchRequest(searchParams),
                Aggregations = SetupFacets(searchParams.FacetFields),
                Sort = SetSortOrder(searchParams.Sort),
                Source = searchParams.IncludeSource,
                Scroll = searchParams.Scroll,
                Size = searchParams.PageSize,
                From = searchParams.PagingOffset
            };

            if(searchParams.PageSize != null)
            {
                _searchRequest.Size = searchParams.PageSize;
            }

            try
            {
                var searchResponse = await _openSearchClient.SearchAsync<T>(_searchRequest);
                RaiseExceptionIfResponseIsInvalid(searchResponse);
                return searchResponse;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public IGetResponse<T> Get(string id)
        {
            IGetResponse<T> getResponse =  _openSearchClient.Get<T>(id);
            RaiseExceptionIfResponseIsInvalid(getResponse);
            return getResponse;
        }

        public async Task<IGetResponse<T>> GetAsync(string id)
        {
            IGetResponse<T> getResponse = await _openSearchClient.GetAsync<T>(id);
            RaiseExceptionIfResponseIsInvalid(getResponse);
            return getResponse;
        }

        public async Task<MultiGetResponse> MultiGetAsync(string[] ids)
        {
            MultiGetResponse multiGetResponse = await _openSearchClient.MultiGetAsync(m => m.GetMany<T>(ids));
            RaiseExceptionIfResponseIsInvalid(multiGetResponse);
            return multiGetResponse;
        }

        public IndexResponse IndexDocument(T documentToIndex, bool useInmemoryIndex)
        {
            IndexResponse indexResponse = null;

            if (useInmemoryIndex)
            {
                string inMemoryIndexName = _parameters.InMemoryIndexName;

                if (String.IsNullOrWhiteSpace(_inMemoryIndexName))  
                {
                    // TODO: Check if index exists and create if not.  But be aware of overhead of checking
                    // index existance on each request.  Possibly a timer or similar to check - or can we rely
                    // on it always being there after we create it and store the name?

                    IndexSettings indexSettingsInMemory = CreateInMemoryIndexSettings();
                    
                    ICreateIndexRequest inMemoryIndexRequest = new CreateIndexRequest(inMemoryIndexName);
                    inMemoryIndexRequest.Settings = indexSettingsInMemory;
                    var inMemoryIndex = _openSearchClient.Indices.Create(inMemoryIndexRequest);
                    _inMemoryIndexName = inMemoryIndex.ApiCall.Uri.PathAndQuery.Substring(1);
                }
                
                indexResponse = _openSearchClientInMemory.IndexDocument(documentToIndex);
                //Required since the document is not immediately visible by default.
                // TODO May be possible in one call - see:
                // https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-refresh.html
                // But not sure  if NEST client supports this?
                _openSearchClientInMemory.Indices.Refresh(inMemoryIndexName);  
            }
            else
            {
                indexResponse = _openSearchClient.IndexDocument(documentToIndex);
            }
            
            return indexResponse;
        }

        public IList<CategorisationResult> CategoryMultiSearch(QueryBase baseOrIdsQuery, IList<Category> sourceCategories, bool useInMemoryIndex, bool includeScores, int maxConcurrentQueries)
        {
            // TODO: May want to throw exception if document not found in index.  But can't just do so if no categories match
            // as this may be a legitimate outcome.  Possibly add additional get query just to get the coument and throw exception
            // if no result.
            string indexToUse = useInMemoryIndex ? _inMemoryIndexName : _parameters.IndexDatabase;
            IOpenSearchClient targetOpenSearchClient = useInMemoryIndex  ? _openSearchClientInMemory : _openSearchClient;

            MultiSearchRequest multiSearchRequest = BuildMultiSearchRequest(baseOrIdsQuery: baseOrIdsQuery, sourceCategories: sourceCategories, indexName: indexToUse,
                includeScores: includeScores, maxConcurrent: maxConcurrentQueries);

            MultiSearchResponse topLevelResponse = targetOpenSearchClient.MultiSearch(multiSearchRequest);  // TODO : Async?

            if(!topLevelResponse.IsValid)
            {
                Console.WriteLine("Top level multisearch response was invalid.");
                Console.WriteLine(topLevelResponse.OriginalException.Message);
            }

            IEnumerable<ISearchResponse<T>> responsesInMemory = topLevelResponse.GetResponses<T>();

            IList<CategorisationResult> matchedCategories = new List<CategorisationResult>();

            foreach (string categoryId in multiSearchRequest.Operations.Keys)
            {
                var subResponse = topLevelResponse.GetResponse<T>(categoryId);

                if (subResponse == null)
                {
                    Console.WriteLine("Error retrieving sub response on multisearch for category " + categoryId);
                }

                IHit<T> hit = subResponse.Hits.SingleOrDefault();
                if (hit != null)
                {
                    Category matchedCategory = sourceCategories.Single(c => c.Id == categoryId);
                    var categoryResult = new CategorisationResult(matchedCategory, includeScores ? hit.Score : null);
                    matchedCategories.Add(categoryResult);
                }
            }

            return matchedCategories;
        }

        // TODO: Possibly manage internally and/or async, instead of returning to caller - to test performance.
        public void DeleteDocumentFromIndex(string documentId, bool useInMemoryIndex)
        {
            string targetIndex = useInMemoryIndex ? _inMemoryIndexName : _parameters.IndexDatabase;
            DeleteRequest request = new DeleteRequest(targetIndex, documentId);
             new Thread(() => 
                 {
                     // TODO: In high performance scenarios we don't want the caller
                     // to wait for confirmation.  But we do need to know about errors.
                     // Log delete error if it occurs and signal the caller.
                     DeleteResponse response = _openSearchClient.Delete(request);
                     //RaiseExceptionIfResponseIsInvalid(response);
                 }
             ).Start();
        }

        private QueryContainer SetupSearchRequest(OpenSearchParameters esSearchParams)
        {
            var booleanQuery = new BoolQuery();
            var mustContainer = new List<QueryContainer>();

            Fields fieldsToSearch = null;

            if (esSearchParams.SearchFields?.Count > 0)
            {
                fieldsToSearch = new Field(esSearchParams.SearchFields.First());
                esSearchParams.SearchFields.GetRange(1, esSearchParams.SearchFields.Count - 1).ForEach(fs => fieldsToSearch.And(new Field(fs)));
            }

            if (!string.IsNullOrWhiteSpace(esSearchParams.Query))
            {
                var qsq = new QueryStringQuery
                {
                    Query = esSearchParams.Query,
                    Fields = fieldsToSearch
                };
                mustContainer.Add(qsq);
            }
            else
            {
                mustContainer.Add(new MatchAllQuery());
            }

            if (esSearchParams.DateMatchQueries?.Count > 0)
            {
                esSearchParams.DateMatchQueries.ForEach(dm => mustContainer.Add(new DateRangeQuery
                {
                    Field = dm.Key,
                    GreaterThanOrEqualTo = dm.Value.ToString("yyyy-MM-dd"),
                    LessThanOrEqualTo = dm.Value.ToString("yyyy-MM-dd")
                }));
            }

            if (esSearchParams.DateRange?.Count > 0)
            {
                esSearchParams.DateRange.ForEach(dr => mustContainer.Add(new DateRangeQuery
                {
                    Field = dr.fieldName,
                    GreaterThanOrEqualTo = dr.fromDate.ToString("yyyy-MM-dd"),
                    LessThanOrEqualTo = dr.toDate.ToString("yyyy-MM-dd")
                }));
            }

            if (esSearchParams.FieldQueries?.Count > 0)
            {
                esSearchParams.FieldQueries.ForEach(fdq => mustContainer.Add(new QueryStringQuery
                {
                    Fields = fdq.Key,
                    Query = fdq.Value
                }));
            }


            booleanQuery.Must = mustContainer;

            if (esSearchParams.FilterQueries?.Count > 0)
            {
                var filters = new List<QueryContainer>();
                esSearchParams.FilterQueries.ForEach(fq => filters.Add(new TermsQuery
                {
                    Field = fq.Key,
                    Terms = fq.Value
                }));

                booleanQuery.Filter = filters;
            }

            return booleanQuery;
        }

        private IList<ISort> SetSortOrder(IDictionary<string, ResultsSortOrder> sortOptions)
        {
            if (sortOptions?.Count == 0) return null;

            var sort = new List<ISort>();

            foreach (var item in sortOptions)
            {
                sort.Add(new FieldSort
                {
                    Field = item.Key,
                    Order = item.Value == ResultsSortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending
                });
            }

            return sort;
        }

        private AggregationDictionary SetupFacets(List<string> facetInputFields)
        {
            if (facetInputFields?.Count == 0) return null;

            var facets = new AggregationDictionary();

            foreach (var fc in facetInputFields)
            {
                facets.Add($"{fc}", new AggregationContainer
                {
                    Terms = new TermsAggregation(fc) { Field = new Field(fc), Size = 100 }
                });
            }
            return facets;
        }

        private void RaiseExceptionIfResponseIsInvalid(IResponse response)
        {
            if (!response.IsValid)
            {
                throw new WebException($@"{response.ServerError}
                                        {Environment.NewLine}Debug information: {response.DebugInformation}
                                        {Environment.NewLine}Reasons: [First] -> {response.ServerError?.Error?.RootCause?.First().Reason}
                [Last] -> {response.ServerError?.Error?.RootCause?.Last().Reason}", response.OriginalException);
            }
        }

        private static IndexSettings CreateInMemoryIndexSettings()
        {
            var indexSettingsInMemory = new IndexSettings();
            indexSettingsInMemory.NumberOfReplicas = 0;
            indexSettingsInMemory.NumberOfShards = 1;
            //indexSettingsInMemory.RefreshInterval = 1; // TODO: 1 = 1 millisecond.  How will this play?
            //settings1.Add("merge.policy.merge_factor", "10");
            //settings1.Add("search.slowlog.threshold.fetch.warn", "1s");
            indexSettingsInMemory.Add("index.store.type", "mmapfs");  //memory mapped file
            indexSettingsInMemory.FileSystemStorageImplementation = FileSystemStorageImplementation.MMap;
            return indexSettingsInMemory;
        }

        private MultiSearchRequest BuildMultiSearchRequest(QueryBase baseOrIdsQuery, IList<Category> sourceCategories, string indexName, bool includeScores = false, long? maxConcurrent = null)
        {
            var multiSearchRequest = new MultiSearchRequest();
            multiSearchRequest.Operations = new Dictionary<string, ISearchRequest>();
            multiSearchRequest.MaxConcurrentSearches = maxConcurrent ?? 10;
            // MaxConcurrentSearches doesn't seem to make much difference, at least not 10 Vs 50 vs null on a 4 core PC...

            foreach (Category categoryItem in sourceCategories)
            {
                var categoryQuery = new QueryStringQuery
                {
                    Query = categoryItem.Query
                };

                var queryContainer = new QueryContainer[] { baseOrIdsQuery, categoryQuery };

                var searchRequest = new SearchRequest<T>(indexName)
                {
                    From = 0,
                    Size = 1,   // 1 presumably ?
                    TrackScores = includeScores,
                    TrackTotalHits = false,

                    Query = new BoolQuery
                    {
                        Filter = includeScores ? null : queryContainer,
                        Must = includeScores ? queryContainer : null
                        // TODO: Does Ids or Term query give better performance/response? What about short circuiting and caching?
                    }
                };

                multiSearchRequest.Operations.Add(categoryItem.Id, searchRequest);
            }

            return multiSearchRequest;
        }

        public async Task<ISearchResponse<T>> ScrollAsync(int scrollTimeout, string scrollId)
        {
            ISearchResponse<T> loopingResponse = await _openSearchClient.ScrollAsync<T>(scrollTimeout, scrollId);

            return loopingResponse;
        }

        public async Task<ClearScrollResponse> ClearScroll(string scrollId)
        {
            ClearScrollResponse response = await _openSearchClient.ClearScrollAsync(new ClearScrollRequest(scrollId));
            return response;
        }
    }
}