using NationalArchives.Taxonomy.Common.BusinessObjects;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public class CategoryWithOpenSearchQuery : Category
    {
        private Query _parsedQuery;

        //TODO Replace Query with appropriate Elastic Search class...
        public CategoryWithOpenSearchQuery(Category category, Query parsedQuery) : base()
        {
            this._parsedQuery = parsedQuery;
        }

        public Query ParsedQuery
        {   get => _parsedQuery ;
            set => this._parsedQuery = value;
        }
    }
}
