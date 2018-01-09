using System;

namespace Perpetuum.EntityFramework
{
    public delegate T DynamicPropertyChangingEventHandler<T>(IDynamicProperty<T> property, T newValue);
    
    public interface IDynamicProperty<T>
    {
        T Value { get; set; }
        bool HasValue { get; }
        void Clear();

        event DynamicPropertyChangingEventHandler<T>  PropertyChanging;
        event Action<IDynamicProperty<T>> PropertyChanged;
    }
}