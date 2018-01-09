using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones.Scanning.Modules
{
    public abstract class ItemScannerModule : ActiveModule
    {
        protected ItemScannerModule() : base(true)
        {
            
        }

        protected override void OnAction()
        {
            var unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);
            var probability = GetProbability(unitLock.Target);

            var scannedItems = ScanItems(unitLock.Target).Where(i => FastRandom.NextDouble() <= probability).ToArray();

            var packet = BuildScanResultPacket(unitLock.Target, scannedItems, probability);
            var player = (Player) ParentRobot;
            Debug.Assert(player != null, "player != null");
            player.Session.SendPacket(packet);

            OnTargetScanned(player,unitLock.Target);
        }

        private double GetProbability(Unit target)
        {
            var probability = 1.0;
            probability = ModifyValueByOptimalRange(target,probability);
            return probability;
        }

        protected abstract IEnumerable<ItemInfo> ScanItems(Unit target);
        protected abstract Packet BuildScanResultPacket(Unit target, ItemInfo[] scannedItems, double probability);
        protected abstract void OnTargetScanned(Player player, Unit target);
    }
}