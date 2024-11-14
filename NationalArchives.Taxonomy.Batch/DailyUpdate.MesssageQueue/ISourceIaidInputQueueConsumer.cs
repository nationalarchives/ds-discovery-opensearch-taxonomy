using System;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MesssageQueue
{
    interface ISourceIaidInputQueueConsumer : IDisposable
    {

        Task Init(CancellationToken token);

        int IaidCount { get; }
    }
}
