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
    public class EnergyVampireModule : EnergyDispersionModule
    {
        private readonly ItemProperty _energyVampiredAmount;

        public EnergyVampireModule()
        {
            _energyVampiredAmount = new ModuleProperty(this, AggregateField.energy_vampired_amount);
            AddProperty(_energyVampiredAmount);
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

            // ennyit szedunk le a targettol
            var coreAmount = _energyVampiredAmount.Value;
            ModifyValueByReactorRadiation(unitLock.Target,ref coreAmount);
            // optimalrange-el modositjuk
            coreAmount = ModifyValueByOptimalRange(unitLock.Target,coreAmount);

            var coreNeutralized = 0.0;
            var coreTransfered = 0.0;

            if ( coreAmount > 0.0 )
            {
                var targetCore = unitLock.Target.Core;
                unitLock.Target.Core -= coreAmount;
                coreNeutralized = Math.Abs(targetCore - unitLock.Target.Core);

                unitLock.Target.OnCombatEvent(ParentRobot,new EnergyDispersionEventArgs(coreNeutralized));

                // amit sikerult leszedni azt hozzaadjuk a robothoz
                var core = ParentRobot.Core;
                ParentRobot.Core += coreNeutralized;
                coreTransfered = Math.Abs(core - ParentRobot.Core);

                unitLock.Target.AddThreat(ParentRobot, new Threat(ThreatType.EnWar, coreTransfered + 1));
            }

            var packet = new CombatLogPacket(CombatLogType.EnergyVampire, unitLock.Target, ParentRobot, this);
            packet.AppendDouble(coreAmount);
            packet.AppendDouble(coreNeutralized);
            packet.AppendDouble(coreTransfered);
            packet.Send(unitLock.Target,ParentRobot);
        }
    }
}