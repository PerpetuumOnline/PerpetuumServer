using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Perpetuum
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (enumerable == null)
                return;

            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static NotOfTypeHolder<T> NotOf<T>(this IEnumerable<T> source)
        {
            return new NotOfTypeHolder<T>(source);
        }

        public class NotOfTypeHolder<TSource> : IEnumerable<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private List<Predicate<TSource>> _types;

            public NotOfTypeHolder(IEnumerable<TSource> source)
            {
                _source = source;
            }

            public NotOfTypeHolder<TSource> Types<T1, T2, T3>()
            {
                return Type<T1>().Type<T2>().Type<T3>();
            }

            public NotOfTypeHolder<TSource> Types<T1, T2>()
            {
                return Type<T1>().Type<T2>();
            }

            public NotOfTypeHolder<TSource> Type<T>()
            {
                (_types ?? (_types = new List<Predicate<TSource>>())).Add(t => !(t is T));
                return this;
            }

            public IEnumerator<TSource> GetEnumerator()
            {
                return _types == null ? _source.GetEnumerator() : _source.Where(t => _types.All(predicate => predicate(t))).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> finder)
        {
            var index = 0;

            foreach (var item in collection)
            {
                if (finder(item))
                    return index;

                index++;
            }

            return -1;
        }

        public static IEnumerable<T> ConcatSingle<T>(this IEnumerable<T> enumerable, T value)
        {
            return enumerable.Concat(new[] { value });
        }

        [DebuggerStepThrough]
        public static bool IsNullOrEmpty(this IEnumerable enumerable)
        {
            if (enumerable == null)
                return true;

            var e = enumerable.GetEnumerator();
            return !e.MoveNext();
        }

        /// <summary>
        /// Cuts an enumerable into slices
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Slice<T>(this IEnumerable<T> enumerable, int maxItemCount)
        {
            var step = 0;
            IEnumerable<T> result;
            var array = enumerable as T[] ?? enumerable.ToArray();
            while ((result = array.Skip(step++ * maxItemCount).Take(maxItemCount)).Any())
                yield return result;
        }

        public static T RandomElement<T>(this IEnumerable<T> enumerable)
        {
            var e = enumerable as IList<T> ?? enumerable.ToList();
            var count = e.Count;
            if (count == 0)
                return default(T);

            var index = FastRandom.NextInt(0, count - 1);
            return e[index];
        }

        public static IEnumerable<T> RandomElement<T>(this IEnumerable<T> enumerable,int count)
        {
            var e = enumerable as T[] ?? enumerable.ToArray();
            for (var i = 0; i < count; i++)
            {
                yield return e.RandomElement();
            }
        }

        public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, double> weightSelector)
        {
            var totalWeight = 0.0;
            var result = default(T);

            foreach (var item in sequence)
            {
                var weight = weightSelector(item);
                var r = FastRandom.NextDouble(0, totalWeight + weight);
                if (r >= totalWeight)
                    result = item;

                totalWeight += weight;
            }

            return result;
        }

        public static double Percent<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var count = 0;
            var total = 0;
            foreach (var item in enumerable)
            {
                ++count;
                if (predicate(item))
                {
                    total++;
                }
            }

            return (double)total / count;
        }

        /// <summary>
        ///  {1,2,3,4} => "1,2,3,4"
        /// </summary>
        public static string ArrayToString<T>(this IEnumerable<T> enumerable, string separator = ",")
        {
            var array = enumerable as T[] ?? enumerable.ToArray();
            return array.IsNullOrEmpty() ? string.Empty : array.Select(a => a.ToString()).Aggregate((a, c) => a + separator + c);
        }

        /// <summary>
        /// {1,2,3,4} => "'1','2','3','4'"
        /// </summary>
        public static string ArrayToValueString<T>(this IEnumerable<T> enumerable, string separator = ",")
        {
            var arrayofobjects = enumerable as T[] ?? enumerable.ToArray();

            return arrayofobjects.IsNullOrEmpty() ?
                string.Empty :
                arrayofobjects
                    .Select(a =>
                    {
                        if (a == null)
                        {
                            return "NULL";
                        }
                        else
                        {
                            var s = a as string;
                            if (s != null && s.Trim().IsNullOrEmpty())
                            {
                                return "NULL";
                            }
                        }

                        return "'" + a.ToString() + "'";

                    })
                    .Aggregate((a, c) => a + separator + c);
        }

        public static IEnumerable<TKey> GetKeys<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            foreach (var kvp in enumerable)
            {
                yield return kvp.Key;
            }
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        public static Queue<T> ToQueue<T>(this IEnumerable<T> enumerable)
        {
            return new Queue<T>(enumerable);
        }

        public static ConcurrentQueue<T> ToConcurrentQueue<T>(this IEnumerable<T> enumerable)
        {
            return new ConcurrentQueue<T>(enumerable);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            var d = new Dictionary<TKey,TValue>();

            foreach (var kvp in enumerable)
            {
                d[kvp.Key] = kvp.Value;
            }

            return d;
        }


        public static Dictionary<string, object> ToDictionary<T>(this IEnumerable<T> enumerable, string prefix, Converter<T, object> converter)
        {
            var result = new Dictionary<string, object>();

            var index = 0;
            foreach (var item in enumerable)
            {
                result[$"{prefix}{index++}"] = converter(item);
            }

            return result;
        }

        public static NameValueCollection ToNameValueCollecion<TK, TV>(this IEnumerable<KeyValuePair<TK, TV>> pairs)
        {
            var nv = new NameValueCollection();

            foreach (var kvp in pairs)
            {
                nv.Add(kvp.Key.ToString(),kvp.Value.ToString());
            }

            return nv;
        }

    }
}