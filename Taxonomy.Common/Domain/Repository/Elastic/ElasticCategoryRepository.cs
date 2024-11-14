using AutoMapper;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.Elastic;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{


    public class ElasticCategoryRepository :  AbstractElasticRespository<CategoryFromElastic>, ICategoryRepository
    {
        private const int MAX_CATEGORIES = 250;  //TODO: Probably get the count dynamically and cache.

        private static IList<Category> _categories;


        public ElasticCategoryRepository(IConnectElastic<CategoryFromElastic> elasticConnection, IMapper mapper) : base(elasticConnection, mapper)
        {
        }
        public long Count()
        {
            if(_categories == null)
            {
                var awaiter =FindAll().GetAwaiter();
                awaiter.GetResult();
            }
            return _categories.Count;
        }

        public async Task<IList<Category>> FindAll()
        {
            if(_categories != null)
            {
                return _categories;
            }

            try
            {
                var elasticParamsBuilder = new ElasticSearchParamsBuilder();

                var elasticParams = elasticParamsBuilder.GetElasticSearchParameters(pagingOffset: 0, pageSize: MAX_CATEGORIES);

                ISearchResponse<CategoryFromElastic> elasticCategories = await _elasticConnection.SearchAsync(elasticParams);

                var categories = new List<Category>();

                foreach (var item in elasticCategories.Hits)
                {
                    CategoryFromElastic searchResult = item.Source;
                    var result = _mapper.Map<Category>(searchResult);
                    result.Score = item.Score.HasValue ? (double)item.Score : 0;

                    categories.Add(result);
                }

                if (_categories == null)
                {
                    _categories = categories;
                }

                return categories;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                throw;
            }
        }

        public Category FindByCiaid(string ciaid)
        {
            if(String.IsNullOrEmpty(ciaid))
            {
                throw new TaxonomyException("'Ciaid' identifier is required to retrieve a category.");
            }

            if (_categories != null)
            {
               var awaiter = FindAll().GetAwaiter();
               var result = awaiter.GetResult();
            }

            return _categories.Single(c => String.Equals(c.Id, ciaid, StringComparison.InvariantCultureIgnoreCase));
        }

        public Category FindByTitle(string title)
        {
            if (String.IsNullOrEmpty(title))
            {
                throw new TaxonomyException("'title' parameter is required to retrieve a category by title.");
            }

            if (_categories != null)
            {
                var awaiter = FindAll().GetAwaiter();
                var result = awaiter.GetResult();
            }

            return _categories.Single(c => String.Equals(c.Title, title, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Save(Category category)
        {
            throw new NotImplementedException();
        }
    }
}
