using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public interface IUpdateStagingQueueSender : IDisposable
    {
        bool Enqueue(IaidWithCategories item, CancellationToken token);

        void CompleteAdding();

        bool IsAddingCompleted { get; }

        Task<bool> Init(CancellationToken token, Action<int, int> updateQueueProgress);

        IReadOnlyCollection<string> QueueUpdateErrors { get; }
    }
}
