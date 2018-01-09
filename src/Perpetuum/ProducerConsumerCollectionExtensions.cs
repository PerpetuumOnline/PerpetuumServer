using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Perpetuum
{
    public static class ProducerConsumerCollectionExtensions
    {
        public static void Clear<T>(this IProducerConsumerCollection<T> collection)
        {
            T item;
            while (collection.TryTake(out item)) { }
        }

        public static void Clear<T>(this BlockingCollection<T> collection)
        {
            T item;
            while (collection.TryTake(out item)) { }
        }

        public static IEnumerable<T> TakeAll<T>(this IProducerConsumerCollection<T> collection)
        {
            T item;
            while (collection.TryTake(out item))
            {
                yield return item;
            }
        }

        public static void EnqueueMany<T>(this IProducerConsumerCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.TryAdd(item);
            }
        }
    }
}