using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum
{
    public static class CollectionExtensions
    {
        public static bool IsNullOrEmpty(this ICollection collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static bool AddIf<T>(this ICollection<T> collection, IEnumerable<T> items, Predicate<T> predicate)
        {
            var result = false;
            foreach (var item in items.Where(item => predicate(item)))
            {
                collection.Add(item);
                result = true;
            }
            return result;
        }

        public static ICollection<T> AddMany<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (items == null)
                return collection;

            foreach (var item in items)
            {
                collection.Add(item);
            }

            return collection;
        }
    }
}