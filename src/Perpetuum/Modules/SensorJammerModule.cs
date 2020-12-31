using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules
{
    public class SensorJammerModule : ActiveModule
    {
        private readonly ModuleProperty _ecmStrength;

        public SensorJammerModule() : base(true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);
            _ecmStrength = new ModuleProperty(this, AggregateField.ecm_strength);
            AddProperty(_ecmStrength);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnAction()
        {
            var unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);
            var robot = unitLock.Target.ThrowIfNotType<Robot>(ErrorCodes.InvalidTarget);

            var success = false;
            var targetSensorStrength = robot.SensorStrength * FastRandom.NextDouble();
            var rangedEcmStrength = ModifyValueByOptimalRange(robot, _ecmStrength.Value);
            if (targetSensorStrength < rangedEcmStrength)
            {
                robot.ResetLocks();
                robot.AddThreat(ParentRobot, new Threat(ThreatType.Ewar, Threat.SENSOR_DAMPENER));
                success = true;
            }

            var packet = new CombatLogPacket(CombatLogType.Jammer, robot,ParentRobot,this);
            packet.AppendByte(success.ToByte());
            packet.Send(ParentRobot,robot);

            robot.OnCombatEvent(ParentRobot,new SensorJammerEventArgs(success));
        }
    }

    public class SensorJammerEventArgs : CombatEventArgs
    {
        public bool Success { get; private set; }

        public SensorJammerEventArgs(bool success)
        {
            Success = success;
        }
    }
}