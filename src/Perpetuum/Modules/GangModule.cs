using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules
{
    public sealed class GangModule : EffectModule
    {
        private readonly EffectType _effectType;
        private readonly ItemProperty _effectEnhancerAuraRadiusModifier;
        private readonly ItemProperty _effectModifier;

        public GangModule(EffectType effectType, AggregateField effectModifier)
        {
            _effectType = effectType;
            _effectEnhancerAuraRadiusModifier = new ModuleProperty(this, AggregateField.effect_enhancer_aura_radius_modifier);
            AddProperty(_effectEnhancerAuraRadiusModifier);
            _effectModifier = new ModuleProperty(this, effectModifier);
            AddProperty(_effectModifier);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(_effectType)
                         .SetOwnerToSource()
                         .WithPropertyModifier(_effectModifier.ToPropertyModifier())
                         .WithRadiusModifier(_effectEnhancerAuraRadiusModifier.Value);
        }
    }
}