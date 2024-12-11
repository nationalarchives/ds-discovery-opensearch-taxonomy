using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public interface IUpdateStagingQueueReceiver
    {
        Task<List<IaidWithCategories>> GetNextBatchOfResults(ILogger logger, int sqsRequestTimeoutSeconds);
    }
}
