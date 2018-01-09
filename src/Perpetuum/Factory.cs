using System;
using System.Collections.Generic;

namespace Perpetuum
{
    /// <summary>
    /// Factory class to instances of the registered types
    /// </summary>
    public class Factory<TKey, TValue>
    {
        private readonly IDictionary<TKey, Func<TValue>> _creators = new Dictionary<TKey, Func<TValue>>();

        public void RegisterCreator(TKey key, Func<TValue> valueFactory)
        {
            _creators[key] = valueFactory;
        }

        public bool TryCreate(TKey key, out TValue value)
        {
            Func<TValue> creator;
            if (!_creators.TryGetValue(key, out creator))
            {
                value = default(TValue);
                return false;
            }

            value = creator();
            return true;
        }
    }
}

