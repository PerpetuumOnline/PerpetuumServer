using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules.EffectModules
{
    public class RemoteSensorBoosterModule : EffectModule
    {
        private readonly ItemProperty _effectSensorBoosterLockingRangeModifier;
        private readonly ItemProperty _effectSensorBoosterLockingTimeModifier;

        public RemoteSensorBoosterModule() : base(true)
        {
            _effectSensorBoosterLockingRangeModifier = new ModuleProperty(this, AggregateField.effect_sensor_booster_locking_range_modifier);
            AddProperty(_effectSensorBoosterLockingRangeModifier);
            _effectSensorBoosterLockingTimeModifier = new ModuleProperty(this, AggregateField.effect_sensor_booster_locking_time_modifier);
            AddProperty(_effectSensorBoosterLockingTimeModifier);
        }

        protected override bool CanApplyEffect(Unit target)
        {
            if (!ParentIsPlayer() || !(target is Npc))
                return true;

            OnError(ErrorCodes.ThisModuleIsNotSupportedOnNPCs);
            return false;
        }

        protected override void OnApplyingEffect(Unit target)
        {
            target.SpreadAssistThreatToNpcs(ParentRobot,new Threat(ThreatType.Support,Threat.REMOTE_SENSOR_BOOSTER));
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_remote_sensor_boost)
                         .SetSource(ParentRobot)
                         .WithPropertyModifier(_effectSensorBoosterLockingRangeModifier.ToPropertyModifier())
                         .WithPropertyModifier(_effectSensorBoosterLockingTimeModifier.ToPropertyModifier());
        }
    }
}