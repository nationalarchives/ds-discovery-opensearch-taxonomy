using Lucene.Net.Search;
using NationalArchives.Taxonomy.Common.BusinessObjects;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Lucene
{

    
    public class CategoryWithLuceneQuery
    {
        private Category _category;

        public CategoryWithLuceneQuery(Category category, Query parsedQuery)
        {
            //this.Id = category.Id;
            this.ParsedQuery = parsedQuery;
            //this.Title = category.Title;
            //this.Score = category.Score;
            //this.Lock = category.Lock;

            _category = category;
        }

        public string Id { get => _category.Id;  }

        public Query ParsedQuery { get; private set; }

        public string Title { get => _category.Title; }

        public bool Lock { get => _category.Lock; }

        public double Score { get => _category.Score; }

        public Category Category
        {
            get { return _category; }
        }
    } 
}