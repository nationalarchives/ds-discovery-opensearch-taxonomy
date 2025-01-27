﻿using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public interface IUpdateStagingQueueReceiver<T>
    {
        Task<List<T>> GetNextBatchOfResults(ILogger logger, int sqsRequestTimeoutMilliSeconds);
    }
}
