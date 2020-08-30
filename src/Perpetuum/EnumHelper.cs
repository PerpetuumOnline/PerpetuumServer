using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Perpetuum
{
    public static class EnumHelper
    {
        /// <summary>
        /// Get the enum entry from its name
        /// </summary>
        /// <typeparam name="T">enum type</typeparam>
        /// <param name="name">name of enum entry</param>
        /// <returns>Enum entry</returns>
        public static T GetEnumFromName<T>(string name)
        {
            return (T)Enum.Parse(typeof(T), name, true);
        }

        /// <summary>
        /// Get the string Name of the enum entry by value
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">value of the enum entry</param>
        /// <returns>string name of the entry</returns>
        public static string GetEnumName<T>(object value)
        {
            return Enum.GetName(typeof(T), value);
        }

        /// <summary>
        /// Get the enum entry from its value
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">value of enum entry</param>
        /// <returns>Enum</returns>
        public static T GetEnum<T>(object value)
        {
            return GetEnumFromName<T>(GetEnumName<T>(value));
        }

        /// <summary>
        /// Converts an enum to a dictionary
        /// </summary>
        public static Dictionary<string, object> ToDictionary<T>()
        {
            var result = new Dictionary<string, object>();

            var values = Enum.GetValues(typeof(T));

            for (var i = 0; i < values.Length; i++)
            {
                var value = values.GetValue(i);
                var name = Enum.GetName(typeof(T), value);
                Debug.Assert(name != null, "name != null");
                result[name] = Convert.ChangeType(value, Enum.GetUnderlyingType(typeof(T)));
            }

            return result;
        }
    }
}