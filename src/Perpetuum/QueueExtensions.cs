using System.Collections.Generic;

namespace Perpetuum
{
    public static class QueueExtensions
    {
        public static bool TryPeek<T>(this Queue<T> queue, out T item)
        {
            if (queue.Count == 0)
            {
                item = default(T);
                return false;
            }
            item = queue.Peek();
            return true;
        }

        public static bool TryDequeue<T>(this Queue<T> queue, out T item)
        {
            if (queue.Count == 0)
            {
                item = default(T);
                return false;
            }

            item = queue.Dequeue();
            return true;
        }

        public static void EnqueueMany<T>(this Queue<T> queue, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                queue.Enqueue(item);
            }
        }
    }
}