using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class DetectionModule : EffectModule
    {
        private readonly ItemProperty _detectionStrengthModifier;
        private readonly ItemProperty _stealthStrengthModifier;

        public DetectionModule()
        {
            _detectionStrengthModifier = new ModuleProperty(this, AggregateField.effect_detection_strength_modifier);
            AddProperty(_detectionStrengthModifier);
            _stealthStrengthModifier = new ModuleProperty(this,AggregateField.effect_stealth_strength_modifier);
            AddProperty(_stealthStrengthModifier);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_detection)
                                .WithPropertyModifier(_detectionStrengthModifier.ToPropertyModifier())
                                .WithPropertyModifier(_stealthStrengthModifier.ToPropertyModifier());
        }
    }
}
