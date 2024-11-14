using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public interface IOpenSearchIAViewUpdateRepository
    {
        IaidWithCategories GetByDocReference(string docReference);

        void Save(IaidWithCategories iaidWithCategories);

        void SaveAll(IEnumerable<IaidWithCategories> iaidsWithCategories);
    }
}
