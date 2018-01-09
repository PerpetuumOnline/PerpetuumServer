using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Data
{
    public static class DataRecordExtensions
    {
        public static bool Contains(this IDataRecord record, string name)
        {
            return GetNames(record).Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<string> GetNames(this IDataRecord record)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                yield return record.GetName(i);
            }
        }
        public static IDataRecordStepper GetStepper(this IDataRecord record)
        {
            return new DataRecordStepper(record);
        }

        public static T GetValue<T>(this IDataRecord record, int index)
        {
            if (record == null || record.IsDBNull(index))
                return default(T);

            return (T)record.GetValue(index);
        }

        public static T GetValueOrDefault<T>(this IDataRecord record,string name,T defaultValue = default(T))
        {
            if (record.TryGetValue(name, out T value))
                return value;

            return defaultValue;
        }

        public static T GetValueOrDefault<T>(this IDataRecord record,string name,Func<T> valueFactory)
        {
            if (record.TryGetValue(name, out T value))
                return value;

            return valueFactory();
        }

        public static T GetValue<T>(this IDataRecord record, string name)
        {
            var result = default(T);

            if (record != null)
            {
                var index = record.GetOrdinal(name);
                if (!record.IsDBNull(index))
                {
                    result = GetValue<T>(record, (int) index);
                }
            }

            return result;
        }

        public static object GetValue(this IDataRecord record, string name)
        {
            if (record == null)
                return null;

            var index = record.GetOrdinal(name);

            if (record.IsDBNull(index))
                return null;

            return record.GetValue(index);
        }

        public static bool IsDBNull(this IDataRecord record, string name)
        {
            if (record == null)
                return true;

            var index = record.GetOrdinal(name);
            return record.IsDBNull(index);
        }

        public static Dictionary<string, object> RecordsToDictionary(this IEnumerable<IDataRecord> records, string prefix = "r", Converter<KeyValuePair<string, object>, object> valueConverter = null)
        {
            return records.Select((record, index) => new { record, index }).ToDictionary(a => prefix + a.index, a =>
            {
                return (object)a.record.RecordToDictionary(valueConverter);
            });
        }

        public static Dictionary<string, object> RecordToDictionary(this IDataRecord record, Converter<KeyValuePair<string, object>, object> converter = null)
        {
            return ToEnumerable(record).ToDictionary(kvp => kvp.Key, kvp => converter == null ? kvp.Value : converter(kvp));
        }

        public static IEnumerable<KeyValuePair<string, object>> ToEnumerable(this IDataRecord record)
        {
            if (record == null)
                yield break;

            for (var i = 0; i < record.FieldCount; i++)
            {
                var kvp = new KeyValuePair<string, object>(record.GetName(i), GetValue<object>(record, i));
                yield return kvp;
            }
        }

        public static string ToInsertString(this IDataRecord record, string tableName, string exceptColumn =null)
        {
            var dict = RecordToDictionary(record);
            return dict.ToInsertString(tableName, exceptColumn);
        }

        public static bool TryGetValue<T>(this IDataRecord record, string name, out T value)
        {
            if (record == null)
            {
                value = default(T);
                return false;
            }

            if (!Contains(record, name))
            {
                value = default(T);
                return false;
            }

            value = GetValue<T>(record, name);
            return true;
        }
    }
}