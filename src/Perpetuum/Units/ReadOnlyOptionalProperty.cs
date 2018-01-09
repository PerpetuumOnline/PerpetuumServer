namespace Perpetuum.Units
{
    public class ReadOnlyOptionalProperty<T> : IOptionalProperty
    {
        private readonly UnitDataType _type;
        private readonly T _value;

        public ReadOnlyOptionalProperty(UnitDataType type,T value)
        {
            _type = type;
            _value = value;
        }

        public T Value
        {
            get { return _value; }
        }

        public UnitDataType Type
        {
            get { return _type; }
        }

        object IOptionalProperty.Value
        {
            get { return _value; }
        }
    }
}