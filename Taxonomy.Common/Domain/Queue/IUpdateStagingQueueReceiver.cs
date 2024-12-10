using NationalArchives.Taxonomy.Common.BusinessObjects;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public interface IUpdateStagingQueueReceiver
    {

        IList<IaidWithCategories> DequeueIaidsWithCategories(int numberToFetch);

        IaidWithCategories DeQueueNextIaidWithCategories();

        List<IaidWithCategories> DeQueueNextListOfIaidsWithCategories();

        IAsyncEnumerable<List<IaidWithCategories>> IterateResults();
    }
}
