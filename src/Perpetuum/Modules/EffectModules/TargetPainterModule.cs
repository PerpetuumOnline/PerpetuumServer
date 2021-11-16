using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class TargetPainterModule : EffectModule
    {
        private readonly ItemProperty _effectStealthStrengthModifier;

        public TargetPainterModule() : base(true)
        {
            _effectStealthStrengthModifier = new ModuleProperty(this, AggregateField.effect_stealth_strength_modifier);
            AddProperty(_effectStealthStrengthModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_target_painting)
                                .SetSource(ParentRobot)
                                .WithPropertyModifier(_effectStealthStrengthModifier.ToPropertyModifier());
        }
    }
}