using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Units
{
    public delegate void OptionalPropertyChangeEventHandler(IOptionalProperty property);

    public class OptionalPropertyCollection : IEnumerable<IOptionalProperty>
    {
        private readonly Dictionary<UnitDataType,IOptionalProperty> _properties = new Dictionary<UnitDataType, IOptionalProperty>();
        private readonly ConcurrentQueue<IOptionalProperty> _updatedProperties = new ConcurrentQueue<IOptionalProperty>();

        public IOptionalProperty Get(UnitDataType type)
        {
            return _properties.GetOrDefault(type);
        }

        public void Add(IOptionalProperty property)
        {
            _properties[property.Type] = property;

            var n = property as INotifyOptionalPropertyChanged;
            if ( n == null )
                return;
            n.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(IOptionalProperty property)
        {
            _updatedProperties.Enqueue(property);
            PropertyChanged?.Invoke(property);
        }

        public IEnumerable<IOptionalProperty> GetUpdatedProperties()
        {
            return _updatedProperties.TakeAll().Distinct().ToArray();
        }

        public IEnumerator<IOptionalProperty> GetEnumerator()
        {
            return _properties.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event OptionalPropertyChangeEventHandler PropertyChanged;
    }
}