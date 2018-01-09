using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Units
{
    public class UnitProperty : ItemProperty
    {
        protected readonly Unit owner;
        private readonly AggregateField _modifierField;
        private readonly IList<AggregateField> _effectModifiers;

        public UnitProperty(Unit owner, AggregateField field, AggregateField modifierField = AggregateField.undefined, params AggregateField[] effectModifiers) : base(field)
        {
            this.owner = owner;
            _modifierField = modifierField;

            if (effectModifiers == null)
                return;

            _effectModifiers = new List<AggregateField>();

            foreach (var effectModifier in effectModifiers)
            {
                _effectModifiers.Add(effectModifier);
            }
        }

        protected override double CalculateValue()
        {
            var m = owner.GetPropertyModifier(Field);

            if (!_effectModifiers.IsNullOrEmpty())
            {
                foreach (var effectModifier in _effectModifiers)
                {
                    owner.ApplyEffectPropertyModifiers(effectModifier,ref m);
                }
            }

            if (_modifierField == AggregateField.undefined)
                return m.Value;

            var mod = owner.GetPropertyModifier(_modifierField);
            mod.Modify(ref m);

            return m.Value;
        }

        protected override bool IsRelated(AggregateField field)
        {
            if (_modifierField == field)
                return true;

            if (_effectModifiers != null)
            {
                return _effectModifiers.Any(m => m == field);
            }

            return base.IsRelated(field);
        }

    }
}