using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using System;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Service.Interface
{
    public interface IUpdateOpenSearchService
    {
       Task Init();

       Task Flush();
    }
}
