using System;
using System.Collections.Immutable;
using Perpetuum.GenXY;

namespace Perpetuum.EntityFramework
{
    public class EntityDynamicProperties
    {
        private ImmutableDictionary<string, object> _items = ImmutableDictionary<string, object>.Empty;

        public event Action Updated;

        public ImmutableDictionary<string,object> Items
        {
            get { return _items; }
            set { ImmutableInterlocked.Update(ref _items, i => value); }
        }

        public bool Contains(string key)
        {
            return _items.ContainsKey(key);
        }

        public void Remove(string key)
        {
            object value;
            if (ImmutableInterlocked.TryRemove(ref _items, key, out value))
                OnUpdated();
        }

        public void Clear()
        {
            var updated = false;
            ImmutableInterlocked.Update(ref _items, i =>
            {
                updated = false;
                if (i.Count <= 0)
                    return i;

                updated = true;
                return i.Clear();
            });

            if (updated)
                OnUpdated();
        }

        public T GetOrDefault<T>(string key)
        {
            return _items.GetOrDefault<T>(key);
        }

        public T GetOrAdd<T>(string key,T defaultValue)
        {
            return GetOrAdd(key, () => defaultValue);
        }

        public T GetOrAdd<T>(string key)
        {
            return GetOrAdd(key, () => default(T));
        }

        public T GetOrAdd<T>(string key, Func<T> valueCreator)
        {
            var updated = false;
            try
            {
                var x = ImmutableInterlocked.GetOrAdd(ref _items, key, k =>
                {
                    updated = true;
                    return valueCreator != null ? valueCreator() : default(T);
                });

                return (T) x;
            }
            finally
            {
                if (updated)
                    OnUpdated();
            }
        }

        public void Update<T>(string key, T value)
        {
            Set(key, value);
        }

        public void Set<T>(string key, T value)
        {
            var updated = false;
            ImmutableInterlocked.AddOrUpdate(ref _items, key, k =>
            {
                updated = true;
                return value;
            }, (k, v) =>
            {
                updated = false;
                if (Equals(value, v))
                    return v;

                updated = true;
                return value;
            });

            if (updated)
                OnUpdated();
        }

        private void OnUpdated()
        {
            Updated?.Invoke();
        }

        public GenxyString ToGenxyString()
        {
            return GenxyConverter.Serialize(_items);
        }

        public IDynamicProperty<T> GetProperty<T>(string key, Func<T> valueFactory = null)
        {
            return new DynamicProperty<T>(this, key, valueFactory);
        }
    }
}