using System;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class ShieldGeneratorModule : EffectModule
    {
        private readonly ItemProperty _shieldRadius;
        private readonly ModuleProperty _shieldAbsorbtion;

        public ShieldGeneratorModule()
        {
            _shieldRadius = new ModuleProperty(this,AggregateField.shield_radius);
            AddProperty(_shieldRadius);
            _shieldAbsorbtion = new ModuleProperty(this, AggregateField.shield_absorbtion);
            _shieldAbsorbtion.AddEffectModifier(AggregateField.effect_shield_absorbtion_modifier);

            AddProperty(_shieldAbsorbtion);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.shield_absorbtion:
                case AggregateField.shield_absorbtion_modifier:
                case AggregateField.effect_shield_absorbtion_modifier:
                    _shieldAbsorbtion.Update();
                    break;
            }
            
            base.UpdateProperty(field);
        }

        public double AbsorbtionModifier
        {
            get
            {
                var ratio = ParentRobot.SignatureRadius / _shieldRadius.Value;
                ratio = Math.Max(ratio, 1.0);
                var result = (1 / _shieldAbsorbtion.Value) * ratio;
                return result;
            }
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_shield);
        }
    }
}