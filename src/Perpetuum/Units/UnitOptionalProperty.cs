using System;
using Perpetuum.EntityFramework;

namespace Perpetuum.Units
{
    public class UnitOptionalProperty<T> : IOptionalProperty,INotifyOptionalPropertyChanged
    {
        private readonly IDynamicProperty<T> _property;

        public event OptionalPropertyChangeEventHandler PropertyChanged;

        public UnitOptionalProperty(Unit owner, UnitDataType type, string name, Func<T> defaultValueFactory)
        {
            _property = owner.DynamicProperties.GetProperty(name, defaultValueFactory);
            _property.PropertyChanged += PropertyOnPropertyChanged;
            Type = type;
        }

        private void PropertyOnPropertyChanged(IDynamicProperty<T> dynamicProperty)
        {
            PropertyChanged?.Invoke(this);
        }

        public UnitDataType Type { get; private set; }

        object IOptionalProperty.Value
        {
            get { return Value; } 
        }

        public T Value
        {
            get { return _property.Value; }
            set { _property.Value = value; }
        }
    }
}