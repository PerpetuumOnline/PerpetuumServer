using System;

namespace Perpetuum.EntityFramework
{
    internal class DynamicProperty<T> : IDynamicProperty<T>
    {
        private readonly EntityDynamicProperties _properties;
        private readonly Func<T> _valueFactory;

        public DynamicProperty(EntityDynamicProperties properties, string key, Func<T> valueFactory)
        {
            Key = key;
            _properties = properties;
            _valueFactory = valueFactory;
        }

        private string Key { get; set; }

        public T Value
        {
            get { return _properties.GetOrAdd(Key, _valueFactory); }
            set
            {
                var newValue = OnPropertyChanging(value);

                if (Equals(Value, newValue))
                    return;

                _properties.Update(Key, newValue);
                OnPropertyChanged();
            }
        }

        public bool HasValue => _properties.Contains(Key);

        public void Clear()
        {
            _properties.Remove(Key);
        }

        public event DynamicPropertyChangingEventHandler<T> PropertyChanging;

        private T OnPropertyChanging(T newvalue)
        {
            var handler = PropertyChanging;
            return handler != null ? handler(this, newvalue) : newvalue;
        }

        public event Action<IDynamicProperty<T>> PropertyChanged;

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this);
        }

        public override string ToString()
        {
            return $"{Key} = {Value}";
        }
    }
}