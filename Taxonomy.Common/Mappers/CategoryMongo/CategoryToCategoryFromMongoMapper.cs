using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.Mongo;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Mappers
{
    internal class CategoryToCategoryFromMongoMapper : IMapper<Category, CategoryFromMongo>
    {
        public CategoryFromMongo MapFrom(Category input)
        {
            throw new NotImplementedException();
        }
    }
}
