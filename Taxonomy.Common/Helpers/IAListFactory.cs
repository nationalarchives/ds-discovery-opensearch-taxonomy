using AutoMapper;
using NationalArchives.Taxonomy.Common.DataObjects.Elastic;
using NationalArchives.Taxonomy.Common.Domain;
using Nest;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Helpers
{
    internal class IAListFactory<TInput, TOutput> : IPaginatedListFactory<TInput, TOutput> where TInput : class where TOutput : class, ISearchResult
    {
        private ISearchResponse<TInput> _searchResponse;

        private IMapper _mapper;


        public IAListFactory(ISearchResponse<TInput> searchResponse, IMapper mapper)
        {
            _searchResponse = searchResponse;
            _mapper = mapper;
        }

        public PaginatedList<TOutput> CreatePaginatedList(long limit, long offset, double minScore = 0)
        {
            var paginatedList = new PaginatedList<TOutput>();
            var assets = new List<TOutput>();

            foreach (var item in _searchResponse.Hits)
            {
                TInput searchResult = item.Source;
                var result = _mapper.Map<TOutput>(searchResult);
                result.Score = item.Score.HasValue ? (double)item.Score : 0;

                assets.Add(result);
            }

            paginatedList.Results = assets;
            paginatedList.NumberOfResults =  _searchResponse.Total;
            paginatedList.Limit = limit;
            paginatedList.Offset = offset;
            //TODO: Should this be the actual min score in the results, of the requested min score?
            paginatedList.MinimumScore = minScore;

            return paginatedList;
        }
    }
}
