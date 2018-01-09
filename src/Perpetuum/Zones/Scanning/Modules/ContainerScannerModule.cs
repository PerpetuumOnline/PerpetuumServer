using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Zones.Scanning.Modules
{
    public class ContainerScannerModule : ItemScannerModule
    {
        protected override IEnumerable<ItemInfo> ScanItems(Unit target)
        {
            var scanner = new ContainerScanner();
            target.AcceptVisitor(scanner);
            return scanner.ScannedItems;
        }

        protected override Packet BuildScanResultPacket(Unit target, ItemInfo[] scannedItems, double probability)
        {
            var packet = new Packet(ZoneCommand.ScanContainerResult);
            packet.AppendLong(target.Eid);
            packet.AppendLong(Eid);
            packet.AppendByte((byte)(probability * 255));
            packet.AppendInt(scannedItems.Length);

            foreach (var scannedItem in scannedItems)
            {
                packet.AppendInt(scannedItem.Definition);
                packet.AppendInt(scannedItem.Quantity);
            }

            return packet;
        }

        protected override void OnTargetScanned(Player player, Unit target)
        {
            var npc = target as Npc;
            if (npc == null)
                return;

            player.MissionHandler.EnqueueMissionEventInfo(new ScanContainerEventInfo(player,npc,target.CurrentPosition));
            
        }

        private class ContainerScanner : IEntityVisitor<Robot>,IEntityVisitor<Npc>,IEntityVisitor<LootContainer>
        {
            public ContainerScanner()
            {
                ScannedItems = new List<ItemInfo>();
            }

            public IList<ItemInfo> ScannedItems { get; private set; }

            public void Visit(Robot robot)
            {
                var container = robot.GetContainer().ThrowIfNull(ErrorCodes.ContainerNotFound);
                ScannedItems = container.GetItems().Select(l => l.ItemInfo).ToArray();
            }

            public void Visit(Npc npc)
            {
                ScannedItems = npc.LootGenerator.Generate().Select(l => l.ItemInfo).ToArray();
            }

            public void Visit(LootContainer lootContainer)
            {
                ScannedItems = lootContainer.GetLootItems().Select(l => l.ItemInfo).ToArray();
            }
        }
    }
}