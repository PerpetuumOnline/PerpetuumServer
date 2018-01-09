using System.Linq;

namespace Perpetuum
{
    public static class LookupExtensions
    {
        public static TValue[] GetOrEmpty<TKey, TValue>(this ILookup<TKey, TValue> lookup, TKey key)
        {
            return !lookup.Contains(key) ? new TValue[0] : lookup[key].ToArray();
        }

        public static bool TryGetValue<TKey, TValue>(this ILookup<TKey, TValue> lookup, TKey key, out TValue[] values)
        {
            if (!lookup.Contains(key))
            {
                values = null;
                return false;
            }

            values = lookup[key].ToArray();
            return true;
        }
    }
}