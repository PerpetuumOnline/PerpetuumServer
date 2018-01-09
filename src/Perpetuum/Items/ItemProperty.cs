using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Items
{
    public abstract class ItemProperty
    {
        public static readonly ItemProperty None = new NullProperty(AggregateField.undefined);

        public AggregateField Field { get; private set; }
        public double Value { get; private set; }

        protected ItemProperty(AggregateField field)
        {
            Field = field;
        }

        public void SetValue(double value)
        {
            OnPropertyChanging(ref value);

            if (Math.Abs(Value - value) < double.Epsilon)
                return;

            Value = value;

            OnPropertyChanged();
        }

        public void UpdateIfRelated(AggregateField field)
        {
            if (IsRelated(field))
                Update();
        }

        public void Update()
        {
            var v = CalculateValue();
            SetValue(v);
        }

        protected abstract double CalculateValue();

        protected virtual bool IsRelated(AggregateField field)
        {
            return Field == field;
        }

        public override string ToString()
        {
            return $"Field: {Field}, Value: {Value}";
        }

        protected virtual void OnPropertyChanging(ref double newValue)
        {
        }

        public event Action<ItemProperty> PropertyChanged;

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this);
        }


        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                               {k.ID,(int)Field},
                               {k.value,Value}
                       };
        }

        public void AddToDictionary(IDictionary<string, object> dictionary)
        {
            if (dictionary == null || !HasValue)
                return;

            dictionary["a" + (int)Field] = ToDictionary();
        }

        public bool HasValue => Math.Abs(Field.GetDefaultValue() - Value) > double.Epsilon;

        public void AppendToPacket(BinaryStream binaryStream)
        {
            binaryStream.AppendInt((int)Field);
            binaryStream.AppendDouble(Value);
        }

        public ItemPropertyModifier ToPropertyModifier()
        {
            return ItemPropertyModifier.Create(Field, Value);
        }

        private class NullProperty : ItemProperty
        {
            public NullProperty(AggregateField field) : base(field)
            {
            }

            protected override double CalculateValue()
            {
                return 0.0;
            }
        }

        public static string ToDebugString(IEnumerable<ItemProperty> properties)
        {
            var enumerable = properties as ItemProperty[] ?? properties.ToArray();
            if (enumerable.Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("=============");

            foreach (var property in enumerable)
            {
                sb.AppendLine($"{property.Field} = {property.Value}");
            }

            sb.AppendLine("=============");
            return sb.ToString();
        }
    }
}