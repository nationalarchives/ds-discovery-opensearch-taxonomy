using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    public interface IElasticIAViewUpdateRepository
    {
        IaidWithCategories GetByDocReference(string docReference);

        void Save(IaidWithCategories iaidWithCategories);

        void SaveAll(IEnumerable<IaidWithCategories> iaidsWithCategories);
    }
}
