using NationalArchives.Taxonomy.Common.DataObjects.Elastic;
using System;

namespace NationalArchives.Taxonomy.Common.Service.Interface
{
    public interface IUpdateElasticService
    {
        void Init();

        void Flush();
    }
}
