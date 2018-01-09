using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Zones.Scanning.Modules
{
    public class UnitScannerModule : ItemScannerModule
    {
        protected override void OnTargetScanned(Player player, Unit target)
        {
            var npc = target as Npc;
            if (npc == null)
                return;

            player.MissionHandler.EnqueueMissionEventInfo(new ScanUnitEventInfo(player,npc,target.CurrentPosition ));
        }

        protected override IEnumerable<ItemInfo> ScanItems(Unit target)
        {
            var scanner = new UnitScanner();
            target.AcceptVisitor(scanner);
            return scanner.ScannedItems;
        }

        protected override Packet BuildScanResultPacket(Unit target, ItemInfo[] scannedItems, double probability)
        {
            var packet = new Packet(ZoneCommand.ScanUnitResult);
            packet.AppendLong(target.Eid);
            packet.AppendLong(Eid);
            packet.AppendByte((byte)(probability * 255));
            packet.AppendInt(scannedItems.Length);

            foreach (var item in scannedItems)
            {
                packet.AppendInt(item.Definition);
            }

            return packet;
        }

        private class UnitScanner : IEntityVisitor<Unit>,IEntityVisitor<Robot>,IEntityVisitor<Module>
        {
            public UnitScanner()
            {
                ScannedItems = new List<ItemInfo>();
            }

            public IList<ItemInfo> ScannedItems { get; private set; }

            private void AddScannedItem(Item item)
            {
                ScannedItems.Add(item.ItemInfo);
            }

            public void Visit(Unit unit)
            {
                AddScannedItem(unit);
            }

            public void Visit(Robot robot)
            {
                AddScannedItem(robot);
                robot.VisitModules(this);
            }

            public void Visit(Module module)
            {
                AddScannedItem(module);
            }
        }
    }
}