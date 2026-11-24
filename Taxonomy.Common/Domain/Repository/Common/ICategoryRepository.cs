using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
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
        Task<IList<Category>> FindAll(bool forceRefresh = false);

        /**
         * save new category
         * 
         * @param category
         */
        void Save(Category category);
        Category AddNewCategory(string title, string query, double score = 0);
        Task<IList<Category>> FindCategories(string searchText);
    }

    public interface IRefreshableCategoryRepository
    {
        Task RefreshCategories();
        event EventHandler<EventArgs> CategoriesRefreshed;
    }
}
