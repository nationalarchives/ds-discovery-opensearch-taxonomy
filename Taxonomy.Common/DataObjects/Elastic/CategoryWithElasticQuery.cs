using NationalArchives.Taxonomy.Common.BusinessObjects;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    public class CategoryWithElasticQuery : Category
    {
        private Query _parsedQuery;

        //TODO Replace Query with appropriate Elastic Search class...
        public CategoryWithElasticQuery(Category category, Query parsedQuery) : base()
        {
            this._parsedQuery = parsedQuery;
        }

        public Query ParsedQuery
        {   get => _parsedQuery ;
            set => this._parsedQuery = value;
        }
    }
}
