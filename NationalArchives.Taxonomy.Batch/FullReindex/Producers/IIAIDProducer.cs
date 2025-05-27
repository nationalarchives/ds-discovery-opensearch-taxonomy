using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.FullReindex.Producers
{
    interface IIAIDProducer
    {
        Task InitAsync(CancellationToken token);

        int TotalIdentifiersFetched { get; }

        int CurrentQueueSize { get; }

        string Source {  get; }
    }
}