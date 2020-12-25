using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules.EffectModules
{
    public class WebberModule : EffectModule
    {
        private readonly ItemProperty _effectMassivnesSpeedMaxModifier;

        public WebberModule() : base(true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);
            _effectMassivnesSpeedMaxModifier = new ModuleProperty(this, AggregateField.effect_massivness_speed_max_modifier);
            AddProperty(_effectMassivnesSpeedMaxModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override bool CanApplyEffect(Unit target)
        {
            if (FastRandom.NextDouble() > ModifyValueByOptimalRange(target, 1.0))
            {
                OnError(ErrorCodes.AccuracyCheckFailed);
                return false;
            }

            if (!IsCategory(CategoryFlags.cf_longrange_webber))
                return true;

            var result = GetLineOfSight(target);
            if (!result.hit)
                return true;

            OnError(ErrorCodes.LOSFailed);
            return false;
        }

        protected override void OnApplyingEffect(Unit target)
        {
            target.AddThreat(ParentRobot, new Threat(ThreatType.Debuff, Threat.WEBBER));
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            var effectProperty = _effectMassivnesSpeedMaxModifier.ToPropertyModifier();
            effectProperty.Add(effectBuilder.Owner.Massiveness);

            if (effectProperty.Value >= 1.0)
                effectProperty.ResetToDefaultValue();

            effectBuilder.SetType(EffectType.effect_demobilizer)
                .SetSource(ParentRobot)
                .WithPropertyModifier(effectProperty);
        }
    }


}