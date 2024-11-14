using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Mongo;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Common
{
    public interface ICategoryRepository
    {
        Category FindByCiaid(String ciaid);

        /**
         * find by title
         * 
         * @param ttl
         * @return
         */
        Category FindByTitle(String title);

        /**
         * count number of elements in collection
         * 
         * @return
         */
        long Count();

        /**
         * retrieve all elements from collection
         * 
         * @return
         */
        Task<IList<Category>> FindAll();

        /**
         * save new category
         * 
         * @param category
         */
        void Save(Category category);
    }
}
