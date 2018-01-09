using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules
{
    public class EnergyTransfererModule : ActiveModule
    {
        private readonly ItemProperty _energyTransferAmount;

        public EnergyTransfererModule() : base(true)
        {
            _energyTransferAmount = new ModuleProperty(this,AggregateField.energy_transfer_amount);
            AddProperty(_energyTransferAmount);
        }

        protected override void OnAction()
        {
            var unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            (ParentIsPlayer() && unitLock.Target is Npc).ThrowIfTrue(ErrorCodes.ThisModuleIsNotSupportedOnNPCs);

            if (!LOSCheckAndCreateBeam(unitLock.Target))
            {
                OnError(ErrorCodes.LOSFailed);
                return;
            }

            var coreAmount = _energyTransferAmount.Value;
            coreAmount = ModifyValueByOptimalRange(unitLock.Target,coreAmount);

            var coreNeutralized = 0.0;
            var coreTransfered = 0.0;

            if ( coreAmount > 0.0 )
            {
                var core = ParentRobot.Core;
                ParentRobot.Core -= coreAmount;
                coreNeutralized = Math.Abs(core - ParentRobot.Core);

                var targetCore = unitLock.Target.Core;
                unitLock.Target.Core += coreNeutralized;
                coreTransfered = Math.Abs(targetCore - unitLock.Target.Core);

                unitLock.Target.SpreadAssistThreatToNpcs(ParentRobot,new Threat(ThreatType.Support, coreAmount * 2));
            }

            var packet = new CombatLogPacket(CombatLogType.EnergyTransfer, unitLock.Target, ParentRobot, this);
            packet.AppendDouble(coreAmount);
            packet.AppendDouble(coreNeutralized);
            packet.AppendDouble(coreTransfered);
            packet.Send(unitLock.Target,ParentRobot);
        }
    }
}