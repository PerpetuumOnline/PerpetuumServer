using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class TargetBlinderModule : EffectModule
    {
        private readonly ItemProperty _effectDetectionStrengthModifier;

        public TargetBlinderModule() : base(true)
        {
            _effectDetectionStrengthModifier = new ModuleProperty(this, AggregateField.effect_detection_strength_modifier);
            AddProperty(_effectDetectionStrengthModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_detection)
                                .SetSource(ParentRobot)
                                .WithPropertyModifier(_effectDetectionStrengthModifier.ToPropertyModifier());
        }
    }
}