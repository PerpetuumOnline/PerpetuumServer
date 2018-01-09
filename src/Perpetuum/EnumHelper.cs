using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Perpetuum
{
    public static class EnumHelper
    {
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