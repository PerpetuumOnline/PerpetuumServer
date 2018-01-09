using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Perpetuum
{
    public static class DictionaryExtensions
    {
        public static void Add<TK, TV>(this ConcurrentDictionary<TK, TV> d, TK key, TV value)
        {
            Debug.Assert(!d.ContainsKey(key), "key already exists! (" + key + ")");
            d[key] = value;
        }

        /// <summary>
        /// Safe remove from a concurrent dictionary
        /// </summary>
        public static bool Remove<TK, TV>(this ConcurrentDictionary<TK, TV> d, TK key, out TV value)
        {
            if (d == null)
            {
                value = default(TV);
                return false;
            }

            if (d.TryRemove(key, out value))
                return true;

            var sw = new SpinWait();
            while (d.ContainsKey(key))
            {
                if (d.TryRemove(key, out value))
                    return true;

                sw.SpinOnce();
            }

            value = default(TV);
            return false;
        }

        public static bool Remove<TK, TV>(this ConcurrentDictionary<TK, TV> d, TK key)
        {
            TV value;
            return Remove(d, key, out value);
        }

        public static IEnumerable<TV> GetNonBlockingValues<TK, TV>(this IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            return enumerable.Select(kvp => kvp.Value);
        }

        public static void AddRange<TK, TV>(this IDictionary<TK, TV> source, IEnumerable<KeyValuePair<TK, TV>> collection)
        {
            if (collection == null)
                return;

            foreach (var kvp in collection)
            {
                source[kvp.Key] = kvp.Value;
            }
        }

        public static void RemoveRange<TK, TV>(this IDictionary<TK, TV> source, IEnumerable<TK> collection)
        {
            if (collection == null)
                return;

            using (var e = collection.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    source.Remove(e.Current);
                }
            }
        }

        public static T GetValue<T>(this IDictionary<string, object> dictionary, string key)
        {
            return (T)dictionary[key];
        }

        public static TV GetOrDefault<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, TV defaultValue = default(TV))
        {
            if (dictionary == null)
                return defaultValue;

            if (!dictionary.TryGetValue(key, out TV result))
            {
                result = defaultValue;
            }
            return result;
        }

        public static T GetOrDefault<T>(this IDictionary<string, object> dictionary, string key, T defaultValue = default(T))
        {
            if (dictionary == null)
                return default(T);

            object value;
            if (!dictionary.TryGetValue(key, out value))
            {
                return defaultValue;
            }

            return (T)value;
        }


        public static T GetOrDefault<T>(this IDictionary<string, object> dictionary, string key, Func<T> valueFactory)
        {
            if (dictionary == null)
                return default(T);

            object value;
            if (!dictionary.TryGetValue(key, out value))
            {
                if (valueFactory == null)
                    return default(T);

                return valueFactory();
            }

            return (T)value;
        }

        public static bool TryGetValue<TK, TV, T>(this IDictionary<TK, TV> dictionary, TK key, out T value) where T : TV
        {
            if (dictionary == null)
            {
                value = default(T);
                return false;
            }
            TV currValue;
            if (!dictionary.TryGetValue(key, out currValue))
            {
                value = default(T);
                return false;
            }
            value = (T)currValue;
            return true;
        }

        public static bool TryAdd<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, TV value)
        {
            if (dictionary == null)
                return false;

            if (dictionary.ContainsKey(key))
                return false;

            dictionary.Add(key, value);
            return true;
        }

        public static TV GetOrAdd<TK, TV>(this IDictionary<TK, TV> dictionary, TK key) where TV : new()
        {
            var concurrentDictionary = dictionary as ConcurrentDictionary<TK, TV>;
            if (concurrentDictionary != null)
            {
                return concurrentDictionary.GetOrAdd(key, () => new TV());
            }

            return dictionary.GetOrAdd(key, () => new TV());
        }

        public static TV GetOrAdd<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, Func<TV> creator)
        {
            TV value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = creator();
                dictionary.Add(key, value);
            }
            return value;
        }

        public static bool Compare<TK, TV>(this IDictionary<TK, TV> left, Dictionary<TK, TV> right)
        {
            if (left == null && right == null)
                return true;

            if (left == null || right == null)
                return false;

            if (left.Count != right.Count)
                return false;

            foreach (var leftKvP in left)
            {
                TV rightValue;
                if (!right.TryGetValue(leftKvP.Key, out rightValue))
                    return false;

                if (!Equals(leftKvP.Value, rightValue))
                    return false;
            }
            return true;
        }

        public static void AddOrUpdate<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, TV newValue, Func<TV, TV> updateFunc)
        {
            if (dictionary == null)
                return;

            TV currentValue;
            if (!dictionary.TryGetValue(key, out currentValue))
            {
                dictionary.Add(key, newValue);
                return;
            }

            dictionary[key] = updateFunc(currentValue);
        }

        public static void AddOrUpdate<TK, TV>(this Dictionary<TK, TV> dictionary, TK key, Func<TV> newValueFunc, Func<TV, TV> updateFunc)
        {
            TV currentValue;
            if (!dictionary.TryGetValue(key, out currentValue))
            {
                var newValue = newValueFunc();
                dictionary.Add(key, newValue);
                return;
            }

            dictionary[key] = updateFunc(currentValue);
        }

        public static string ToInsertString(this IEnumerable<KeyValuePair<string, object>> dictionary, string tableName, string exceptKey = null)
        {
            var listKeys = new List<string>();
            var listValues = new List<object>();

            foreach (var pair in dictionary)
            {
                if (exceptKey != null && pair.Key == exceptKey) continue;

                listKeys.Add(pair.Key);
                listValues.Add(pair.Value);

            }

            return "insert " + tableName + " (" + listKeys.ArrayToString() + ") values (" + listValues.ArrayToValueString() + ")";
        }

        public static IEnumerable<TValue> GetValues<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys)
        {
            foreach (var index in keys)
            {
                TValue item;
                if (dictionary.TryGetValue(index, out item))
                {
                    yield return item;
                }
            }
        }

        public static string ToDebugString<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
        {
            if (dictionary == null)
                return string.Empty;

            var builder = new StringBuilder();

            builder.Append('{');

            var first = true;

            foreach (var kvp in dictionary)
            {
                if (first) first = false;
                else
                {
                    builder.Append(',');
                }

                builder.Append(kvp.Key + "=" + kvp.Value);
            }

            builder.Append('}');

            return builder.ToString();
        }

        public static IDictionary<TK, TV> ToReadOnlyDictionary<TK, TV>(this IDictionary<TK, TV> dictionary)
        {
            return new ReadOnlyDictionary<TK, TV>(dictionary);
        }

        public static IDictionary<string, object> ToDictionary(this IDictionary dictionary)
        {
            if (dictionary == null)
                return null;

            var result = new Dictionary<string, object>();

            foreach (DictionaryEntry entry in dictionary)
            {
                result[entry.Key.ToString()] = entry.Value;
            }

            return result;
        }


    }
}