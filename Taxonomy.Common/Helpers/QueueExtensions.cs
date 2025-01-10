using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Helpers
{
    internal static class QueueExtensions
    {
        public static IEnumerable<T> DequeueChunk<T>(this ConcurrentQueue<T> queue, int chunkSize)
        {
            for (int i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                T nextItem;
                bool itemFound = queue.TryDequeue(out nextItem);
                if (nextItem != null)
                {
                    yield return nextItem; 
                }
            }
        }
    }
}
