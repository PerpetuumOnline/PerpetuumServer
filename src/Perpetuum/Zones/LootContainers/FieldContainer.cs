using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Transactions;
using GenXY.Framework.Builders;
using GenXY.Framework.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.ExtensionMethods;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.ExtensionMethods;

namespace Perpetuum.Zones.LootContainers
{
    public class FieldContainer : LootContainer
    {
        public new static readonly TimeSpan DespawnTime = TimeSpan.FromHours(1);

        public FieldContainer(EntityDefault entityDefault, ILootItemRepository lootItemRepository, TimeSpan despawnTime) : base(entityDefault, lootItemRepository, despawnTime)
        {
        }

        protected override void HasAccess(Player looter, int pinCode)
        {
            looter.Character.IsPrivilegedTransactionsAllowed().ThrowIfError();
            base.HasAccess(looter, pinCode);
        }

        public void PutLoots(Player player, int pinCode, IList<KeyValuePair<long, int>> items)
        {
            HasAccess(player, pinCode);

            Zone.CreateBeam(BeamType.loot_bolt,b => b.WithSource(player)
                .WithTarget(this)
                .WithState(BeamState.Hit).WithDuration(1000));

            lock (syncObject)
            {
                using (var scope = DbQuery.CreateTransaction())
                {
                    var container = player.GetContainer();
                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();
                    var progressPacketBuilder = new LootContainerProgressInfoPacketBuilder(container, this, items.Count);

                    var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.PutLoot).SetCharacter(player.Character).SetContainer(Eid);

                    foreach (var kvp in items)
                    {
                        try
                        {
                            var itemEid = kvp.Key;
                            var quantity = kvp.Value;

                            var tmpItem = container.GetItemOrThrow(itemEid);

                            //robotot lehet
                            if (tmpItem.ED.AttributeFlags.NonStackable)
                                continue;

                            if (tmpItem is VolumeWrapperContainer)
                                continue;

                            lock (container)
                            {
                                var resultItem = container.RemoveItem(tmpItem, quantity);
                                if ( resultItem == null )
                                    continue;

                                Repository.Delete(resultItem);
                                b.SetItem(resultItem);
                                Character.LogTransaction(b);

                                //sikerult minden mehet bele a kontenerbe
                                AddLoot(LootItemBuilder.Create(resultItem).Build());
                            }
                        }
                        finally
                        {
                            SendPacketToLooters(progressPacketBuilder);
                            progressPacketBuilder.Increase();
                        }
                    }

                    Repository.Save(container);

                    Transaction.Current.OnCompleted((c) =>
                    {
                        SendLootListToLooters();
                        container.SendUpdateToOwnerAsync();
                        SendPacketToLooters(progressPacketBuilder);
                    });

                    scope.Complete();
                }
            }
        }

        protected override bool CanRemoveIfEmpty()
        {
            return false;
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            if (enterType == ZoneEnterType.Deploy)
            {
                Player player;
                if (Zone.TryGetPlayer(this.GetOwnerAsCharacter(), out player))
                {
                    player.Session.SendPacket(new PinCodePacketBuilder(this));
                }
            }
            
            base.OnEnterZone(zone, enterType);
        }

        private class PinCodePacketBuilder : IBuilder<Packet>
        {
            private readonly FieldContainer _container;

            public PinCodePacketBuilder(FieldContainer container)
            {
                _container = container;
            }

            public Packet Build()
            {
                var packet = new Packet(ZoneCommand.FieldContainerPin);
                packet.AppendLong(_container.Eid);
                packet.AppendInt(_container.PinCode);
                return packet;
            }
        }

    }
}