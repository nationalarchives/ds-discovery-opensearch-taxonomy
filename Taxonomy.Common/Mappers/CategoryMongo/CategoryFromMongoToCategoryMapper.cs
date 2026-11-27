using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.Mongo;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Mappers
{
    public class CategoryFromMongoToCategoryMapper : IMapper<CategoryFromMongo, Category>
    {
        public Category MapFrom(CategoryFromMongo input)
        {
            throw new NotImplementedException();
        }
    }
}
