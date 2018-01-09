using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Perpetuum.Data
{
    /// <summary>
    /// Creates and caches a dynamic sql command
    /// </summary>
    public static class DynamicSqlQuery
    {
        private const string CMD_INSERT = "INSERT";
        private const string CMD_INSERT_WITH_IDENTITY = "INSERT_IDENTITY";
        private const string CMD_UPDATE = "UPDATE";

        private static readonly ConcurrentDictionary<int, string> _cachedCommands = new ConcurrentDictionary<int, string>();

        private static int CalculateHashCode(string command,params object[] objects)
        {
            var result = command.GetHashCode();

            var index = 0;
            while (index < objects.Length)
            {
                var o = objects[index];
                if ( o == null )
                    continue;

                unchecked
                {
                    var dictionary = o as IDictionary<string, object>;
                    if (dictionary != null)
                    {
                        result = dictionary.Keys.Aggregate(result, (current, key) => (current * 397) ^ key.GetHashCode());
                    }
                    else
                    {
                        result = (result*397) ^ o.GetHashCode();
                    }
                }
                index++;
            }

            return result;
        }

        private static IDictionary<string, object> ToDictionary(object o)
        {
            var result = new Dictionary<string, object>();

            if (o == null)
                return result;

            foreach (var propertyInfo in o.GetType().GetProperties())
            {
                result[propertyInfo.Name] = propertyInfo.GetValue(o, null);
            }

            return result;
        }


        public static int Insert(string table ,object o)
        {
            var dictionary = ToDictionary(o);
            var sqlCmd = _cachedCommands.GetOrAdd(CalculateHashCode(CMD_INSERT, table, dictionary), _ =>
            {
                var keys = dictionary.GetKeys().ArrayToString();
                var values = dictionary.GetKeys().Select(k => "@" + k).ArrayToString();
                return $"INSERT INTO {table} ({keys}) VALUES ({values})";
            });

            return Db.Query().CommandText(sqlCmd).SetParameters(dictionary).ExecuteNonQuery();
        }

        //select cast(scope_identity() as int


        public static int InsertAndGetIdentity(string table, object o)
        {
            var dictionary = ToDictionary(o);
            var sqlCmd = _cachedCommands.GetOrAdd(CalculateHashCode(CMD_INSERT_WITH_IDENTITY, table, dictionary), _ =>
            {
                var keys = dictionary.GetKeys().ArrayToString();
                var values = dictionary.GetKeys().Select(k => "@" + k).ArrayToString();
                return $"INSERT INTO {table} ({keys}) VALUES ({values});SELECT cast(scope_identity() as int)";
            });

            return Db.Query().CommandText(sqlCmd).SetParameters(dictionary).ExecuteScalar<int>();
        }


        public static int Update(string table,object columns,object where = null)
        {
            var columnsDictionary = ToDictionary(columns);
            var whereDictionary = ToDictionary(where);

            Debug.Assert(columnsDictionary.Any());
            var sqlCmd = _cachedCommands.GetOrAdd(CalculateHashCode(CMD_UPDATE, table, columnsDictionary, whereDictionary), _ =>
            {
                var commandText = "UPDATE {0} SET ";

                commandText += columnsDictionary.Keys.Select(k => k + " = @" + k).ArrayToString();

                if (whereDictionary.Count > 0)
                {
                    commandText += " WHERE ";

                    var first = true;
                    foreach (var key in whereDictionary.Keys)
                    {
                        if (first) first = false;
                        else commandText += " AND ";
                        commandText += string.Format("{0} = @{0}", key);
                    }
                }

                return string.Format(commandText, table);
            });

            return Db.Query().CommandText(sqlCmd).SetParameters(columnsDictionary.Concat(whereDictionary)).ExecuteNonQuery();
        }
    }
}
