using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules.EffectModules
{
    public class SensorBoosterModule : EffectModule
    {
        private readonly ItemProperty _effectSensorBoosterLockingRangeModifier;
        private readonly ItemProperty _effectSensorBoosterLockingTimeModifier;

        public SensorBoosterModule()
        {
            _effectSensorBoosterLockingRangeModifier = new ModuleProperty(this, AggregateField.effect_sensor_booster_locking_range_modifier);
            AddProperty(_effectSensorBoosterLockingRangeModifier);
            _effectSensorBoosterLockingTimeModifier = new ModuleProperty(this, AggregateField.effect_sensor_booster_locking_time_modifier);
            AddProperty(_effectSensorBoosterLockingTimeModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_sensor_boost)
                                 .WithPropertyModifier(_effectSensorBoosterLockingRangeModifier.ToPropertyModifier())
                                 .WithPropertyModifier(_effectSensorBoosterLockingTimeModifier.ToPropertyModifier());
        }


        protected override void OnApplyingEffect(Unit target)
        {
            ParentRobot.SpreadAssistThreatToNpcs(ParentRobot,new Threat(ThreatType.Buff,Threat.SENSOR_BOOSTER));
            base.OnApplyingEffect(target);
        }
    }
}