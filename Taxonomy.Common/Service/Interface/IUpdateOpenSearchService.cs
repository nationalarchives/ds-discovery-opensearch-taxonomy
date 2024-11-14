using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using System;

namespace NationalArchives.Taxonomy.Common.Service.Interface
{
    public interface IUpdateOpenSearchService
    {
        void Init();

        void Flush();
    }
}
