using System;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules
{
    public class EnergyNeutralizerModule : EnergyDispersionModule
    {
        private readonly ItemProperty _energyNeutralizedAmount;

        public EnergyNeutralizerModule() 
        {
            _energyNeutralizedAmount = new ModuleProperty(this,AggregateField.energy_neutralized_amount);
            AddProperty(_energyNeutralizedAmount);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnAction()
        {
            var unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            if (!LOSCheckAndCreateBeam(unitLock.Target))
            {
                OnError(ErrorCodes.LOSFailed);
                return;
            }

            var coreNeutralized = _energyNeutralizedAmount.Value;
            var coreNeutralizedDone = 0.0;

            ModifyValueByReactorRadiation(unitLock.Target,ref coreNeutralized);
            coreNeutralized = ModifyValueByOptimalRange(unitLock.Target,coreNeutralized);
            
            if ( coreNeutralized > 0.0 )
            {
                var core = unitLock.Target.Core;
                unitLock.Target.Core -= coreNeutralized;
                coreNeutralizedDone = Math.Abs(core - unitLock.Target.Core);

                unitLock.Target.OnCombatEvent(ParentRobot, new EnergyDispersionEventArgs(coreNeutralizedDone));
                var threatValue = (coreNeutralizedDone / 2) + 1;
                unitLock.Target.AddThreat(ParentRobot, new Threat(ThreatType.EnWar, threatValue));
            }

            var packet = new CombatLogPacket(CombatLogType.EnergyNeutralize, unitLock.Target, ParentRobot, this);
            packet.AppendDouble(coreNeutralized);
            packet.AppendDouble(coreNeutralizedDone);
            packet.Send(unitLock.Target,ParentRobot);
        }
    }
}