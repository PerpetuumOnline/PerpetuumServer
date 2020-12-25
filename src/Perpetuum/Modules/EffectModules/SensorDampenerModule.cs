using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules.EffectModules
{
    public class SensorDampenerModule : EffectModule
    {
        private readonly ItemProperty _ecmStrength;
        private readonly ItemProperty _effectSensorDampenerLockingRangeModifier;
        private readonly ItemProperty _effectSensorDampenerLockingTimeModifier;

        public SensorDampenerModule() : base(true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);

            _ecmStrength = new ModuleProperty(this,AggregateField.ecm_strength);
            AddProperty(_ecmStrength);
            _effectSensorDampenerLockingRangeModifier = new ModuleProperty(this, AggregateField.effect_sensor_dampener_locking_range_modifier);
            AddProperty(_effectSensorDampenerLockingRangeModifier);
            _effectSensorDampenerLockingTimeModifier = new ModuleProperty(this, AggregateField.effect_sensor_dampener_locking_time_modifier);
            AddProperty(_effectSensorDampenerLockingTimeModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override bool CanApplyEffect(Unit target)
        {
            if (FastRandom.NextDouble() <= ModifyValueByOptimalRange(target, 1.0))
            {
                var targetSensorStrength = target.SensorStrength * FastRandom.NextDouble();
                if (targetSensorStrength < _ecmStrength.Value)
                {
                    return true;
                }
            }
            OnError(ErrorCodes.AccuracyCheckFailed);
            return false;
        }

        protected override void OnApplyingEffect(Unit target)
        {
            target.AddThreat(ParentRobot, new Threat(ThreatType.Ewar, Threat.SENSOR_DAMPENER));
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_sensor_supress).SetSource(ParentRobot)
                .WithPropertyModifier(_effectSensorDampenerLockingRangeModifier.ToPropertyModifier())
                .WithPropertyModifier(_effectSensorDampenerLockingTimeModifier.ToPropertyModifier());
        }
    }
}