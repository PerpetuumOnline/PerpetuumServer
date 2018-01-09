using System;
using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Items
{
    public struct ItemPropertyModifier
    {
        private readonly AggregateField _field;
        private readonly AggregateFormula _formula;
        private double _value;

        public ItemPropertyModifier(AggregateField field, AggregateFormula formula, double value) : this()
        {
            _field = field;
            _formula = formula;
            _value = value;
        }

        public AggregateField Field
        {
            get { return _field; }
        }

        public double Value
        {
            get { return _value; }
        }

        public bool HasValue
        {
            get
            {
                return Math.Abs(_field.GetDefaultValue() - _value) > double.Epsilon;
            }
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

        public static ItemPropertyModifier Create(string fieldName,string value)
        {
            var f = (AggregateField) Enum.Parse(typeof (AggregateField), fieldName);
            var v = double.Parse(value);
            return Create(f, v);
        }

        public static ItemPropertyModifier Create(AggregateField field)
        {
            return Create(field, field.GetDefaultValue());
        }

        public static ItemPropertyModifier Create(AggregateField field, double value)
        {
            var formula = field.GetFormula();
            return new ItemPropertyModifier(field,formula,value);
        }

        public void NormalizeExtensionBonus()
        {
            switch (_formula)
            {
                case AggregateFormula.Modifier:
                {
                    _value += 1.0;
                    break;
                }
                case AggregateFormula.Inverse:
                {
                    _value = 1 / (_value + 1);
                    if (_value < 0.1)
                        _value = 0.1;
                    break;
                }
            }
        }

        public void AppendToPacket(BinaryStream binaryStream)
        {
            binaryStream.AppendInt((int)Field);
            binaryStream.AppendDouble(Value);
        }

        public void ResetToDefaultValue()
        {
            _value = _field.GetDefaultValue();
        }

        public void Add(double value)
        {
            _value += value;
        }

        public void Multiply(double mul)
        {
            if (mul > 0)
                _value *= mul;
        }

        public void Modify(ref ItemPropertyModifier targetModifier)
        {
            Modify(this,ref targetModifier);
        }

        public void Modify(ref double targetValue)
        {
            Modify(this,ref targetValue);
        }

        public static void Modify(ItemPropertyModifier source, ref ItemPropertyModifier targetModifier)
        {
            targetModifier = Modify(source, targetModifier);
        }

        public static ItemPropertyModifier Modify(ItemPropertyModifier source, ItemPropertyModifier target)
        {
            if (!source.HasValue)
                return target;

            var v = target.Value;
            Modify(source, ref v);
            return new ItemPropertyModifier(target.Field,target._formula, v);
        }

        public static void Modify(ItemPropertyModifier source, ref double targetValue)
        {
            if (!source.HasValue)
                return;

            switch (source._formula)
            {
                // mod & inverse
                case AggregateFormula.Modifier:
                case AggregateFormula.Inverse:
                {
                    targetValue *= source.Value;
                    break;
                }

                // add
                case AggregateFormula.Add:
                {
                    targetValue += source.Value;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return $"Field: {_field}, Formula: {_formula}, Value: {_value}";
        }
    }
}