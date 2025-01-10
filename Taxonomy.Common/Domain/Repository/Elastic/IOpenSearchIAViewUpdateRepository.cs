using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public interface IOpenSearchIAViewUpdateRepository
    {
        IaidWithCategories GetByDocReference(string docReference);

        Task Save(IaidWithCategories iaidWithCategories);

        Task SaveAll(IEnumerable<IaidWithCategories> iaidsWithCategories);
    }
}
