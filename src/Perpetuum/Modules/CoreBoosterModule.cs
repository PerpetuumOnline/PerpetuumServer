using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.Modules
{
    public class CoreBoosterModule : ActiveModule
    {
        public CoreBoosterModule(CategoryFlags ammoCategoryFlags, bool ranged = false) : base(ammoCategoryFlags, ranged)
        {
        }

        protected override void OnAction()
        {
            var ammo = GetAmmo();
            if (ammo == null)
                return;

            var coreAdded = ammo.GetPropertyModifier(AggregateField.core_added);

            var core = ParentRobot.Core;
            ParentRobot.Core += coreAdded.Value;
            var coreBoosted = Math.Abs(core - ParentRobot.Core);

            var packet = new CombatLogPacket(CombatLogType.CoreBooster, ParentRobot);
            packet.AppendDouble(coreAdded.Value);
            packet.AppendDouble(coreBoosted);
            packet.Send(ParentRobot);

            ConsumeAmmo();
        }

    }
}