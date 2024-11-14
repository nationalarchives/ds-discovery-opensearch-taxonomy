using System.Collections.Concurrent;
using System.Threading;

namespace NationalArchives.Taxonomy.Batch.FullReindex.Queues
{
    /// <summary>
    /// Can be used if required to store IAIDs from a producer, e.g. an Open Search scroll cursor, if we need to fetch them this way
    /// instead of using an existing queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FullReIndexIaidPcQueue<T>
    {
        private const int MAX_SIZE = 20000000;
        private BlockingCollection<T> _iaids;

        public FullReIndexIaidPcQueue() : this(MAX_SIZE)
        {
        }

        public FullReIndexIaidPcQueue(int maxSize = MAX_SIZE)
        {
            _iaids = new BlockingCollection<T>(maxSize);
        }

        public bool Enqueue(T item)
        {
            bool success = _iaids.TryAdd(item);
            return success;

        }

        public bool Enqueue(T[] items)
        {
            bool success = false;
            foreach (T item in items)
            {
                success = Enqueue(item);
                if(!success)
                {
                    break;
                }
            }
            return success;
        }

        public int Count
        {
            get => _iaids.Count; 
        }

        public void CompleteAdding()
        {
            _iaids.CompleteAdding();
        }

        public bool IsAddingCompleted
        {
            get => _iaids.IsAddingCompleted; 
        }

        public bool IsCompleted
        {
            get => _iaids.IsCompleted; 
        }

        public bool TryTake(out T item, CancellationToken ct, int millisecondsTimeout = 0)
        {
            bool success = _iaids.TryTake(out item, millisecondsTimeout, ct);
            return success;
        }

        public bool TryTake(out T item,  int millisecondsTimeout = 0)
        {
            bool success = _iaids.TryTake(out item, millisecondsTimeout);
            return success;
        }
    }
}
