using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class ArmorHardenerModule : EffectModule
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_armor_hardener)
                                .WithPropertyModifier(GetPropertyModifier(AggregateField.effect_resist_kinetic))
                                .WithPropertyModifier(GetPropertyModifier(AggregateField.effect_resist_chemical))
                                .WithPropertyModifier(GetPropertyModifier(AggregateField.effect_resist_thermal))
                                .WithPropertyModifier(GetPropertyModifier(AggregateField.effect_resist_explosive));
        }
    }
}