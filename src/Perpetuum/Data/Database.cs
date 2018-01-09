using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Collections;

namespace Perpetuum.Data
{
    public static class Database
    {
        #region Caching

        /// <summary>
        /// Creates a key -> value sql table cache
        /// </summary>
        public static IDictionary<TKey, TValue> CreateCache<TKey, TValue>(string table, string columnKey, string columnValue)
        {
            return CreateCache<TKey,TValue>(table, columnKey, r => r.GetValue<TValue>(columnValue));
        }

        /// <summary>
        /// Sql table cache factory
        /// </summary>
        public static IDictionary<TKey, TValue> CreateCache<TKey, TValue>(string table, string columnKey, Func<IDataRecord, TValue> valueFactory, Func<IDataRecord, bool> selector = null, Func<TKey, TKey> keyFactory = null)
        {
            return new LazyDictionary<TKey, TValue>(() => LoadTableToDictionary(table, columnKey, valueFactory, selector, keyFactory));
        }

        public static ILookup<TKey, TValue> CreateLookupCache<TKey, TValue>(string table, string columnKey, Func<IDataRecord, TValue> valueFactory, Func<IDataRecord, bool> selector = null)
        {
            return new LazyLookup<TKey, TValue>(() =>
            {
                var records = Db.Query().CommandText("select * from " + table).Execute();
                return records.Where(r => selector == null || selector(r)).ToLookup(r => r.GetValue<TKey>(columnKey), valueFactory);
            });
        }

        private static Dictionary<TKey, TV> LoadTableToDictionary<TKey,TV>(string table, string columnKey, Func<IDataRecord, TV> valueFactory, Func<IDataRecord, bool> selector, Func<TKey, TKey> keyFactory)
        {
            var records = Db.Query().CommandText($"select * from {table}").Execute();

            var result = new Dictionary<TKey, TV>();

            foreach (var record in records)
            {
                if (!(selector?.Invoke(record) ?? true))
                    continue;

                var key = record.GetValue<TKey>(columnKey);

                if (keyFactory != null)
                {
                    key = keyFactory(key);
                }

                var value = valueFactory(record);
                result.Add(key, value);
            }

            return result;
        }

        #endregion
    }
}