using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class StealthModule : EffectModule
    {
        private readonly ItemProperty _effectStealthStrengthModifier;

        public StealthModule()
        {
            _effectStealthStrengthModifier = new ModuleProperty(this,AggregateField.effect_stealth_strength_modifier);
            AddProperty(_effectStealthStrengthModifier);
        }

        private double CalculateCorePerTick()
        {
            return CoreUsage * ParentRobot.SignatureRadius;
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_stealth)
                                .WithCorePerTick(CalculateCorePerTick())
                                .WithPropertyModifier(_effectStealthStrengthModifier.ToPropertyModifier());
        }
    }
}