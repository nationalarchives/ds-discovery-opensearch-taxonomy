using NationalArchives.Taxonomy.Common.BusinessObjects;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public interface IUpdateStagingQueueReceiver
    {
        IAsyncEnumerable<List<IaidWithCategories>> IterateResults();
    }
}
