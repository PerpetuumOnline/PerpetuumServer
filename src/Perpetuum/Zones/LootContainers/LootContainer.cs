using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using GenXY.Framework;
using GenXY.Framework.Builders;
using GenXY.Framework.Collections;
using GenXY.Framework.Data;
using GenXY.Framework.Geometry;
using GenXY.Framework.Log;
using GenXY.Framework.Timers;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.ExtensionMethods;
using Perpetuum.Groups.Gangs;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Players.ExtensionMethods;
using Perpetuum.Robots;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.ExtensionMethods;

namespace Perpetuum.Zones.LootContainers
{
    public enum LootContainerType
    {
        LootOnly,
        Field,
        Mission
    }

    public class MissionContainer : LootContainer
    {
        public MissionContainer(EntityDefault entityDefault, ILootItemRepository lootItemRepository, TimeSpan despawnTime) : base(entityDefault, lootItemRepository, despawnTime)
        {
        }
    }

    public class LootContainer : Unit
    {
        public static readonly TimeSpan DespawnTime = TimeSpan.FromMinutes(15);

        private readonly ConcurrentDictionary<Character,int> _pinTryCounts = new ConcurrentDictionary<Character, int>();

        private const int LOOT_RANGE = 10;
        private readonly UnitDespawnHelper _despawnHelper;
        private readonly ILootItemRepository _itemRepository;
        private readonly Looters _looters;
        private readonly IBuilder<Packet> _lootListPacketBuilder;

        private readonly IntervalTimer _timerResetOwner = new IntervalTimer(5 * TimeConstants.MINUTE);
        protected readonly object syncObject = new object();

        private readonly IDynamicProperty<int> _pinCode;

        public LootContainer(EntityDefault entityDefault, ILootItemRepository lootItemRepository, TimeSpan despawnTime) : base(entityDefault)
        {
            _looters = new Looters(this);
            _itemRepository = lootItemRepository;

            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
            _despawnHelper.CanApplyDespawnEffect = OnCanApplyDespawnEffect;

            _lootListPacketBuilder = new LootListPacketBuilder(this, _itemRepository);

            _pinCode = DynamicProperties.GetProperty<int>(k.pinCode);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public int PinCode
        {
            protected get { return _pinCode.Value;}
            set { _pinCode.Value = value; }
        }

        public override ErrorCodes IsAttackable
        {
            get { return ErrorCodes.TargetIsNonAttackable; }
        }

        private bool OnCanApplyDespawnEffect(Unit unit)
        {
            return _looters.Count > 0;
        }

        public IEnumerable<LootItem> GetLootItems()
        {
            return _itemRepository.GetAll(this);
        }

        public void AddLoots(IEnumerable<LootItem> items)
        {
            foreach (var item in items)
            {
                AddLoot(item);
            }
        }

        protected void AddLoot(LootItem item)
        {
            if ( item.Quantity == 0 )
                return;

            _itemRepository.AddWithStack(this,item);
        }

        public void SendLootListToPlayer(Player player, int pinCode)
        {
            HasAccess(player,pinCode);

            Zone.CreateBeam(BeamType.loot_bolt,b => b.WithSource(player)
                .WithTarget(this)
                .WithState(BeamState.Hit).WithDuration(1000));
            player.Session.SendPacket(_lootListPacketBuilder);

            _looters.Add(player);
        }

        protected virtual void HasAccess(Player looter, int pinCode)
        {
            IsInLootRange(looter).ThrowIfFalse(ErrorCodes.LootContainerOutOfRange);

            var owner = this.GetOwnerAsCharacter();
            
            if (owner == Character.None || // van owner?
                 owner == looter.Character || // ugyanaz akar-e lootolni aki a gazdi
                 Gang.CompareGang(looter.Character, owner) // ugyanabban a gangben vannak-e
                )
                return;

            if (!IsFieldContainer() && !looter.IsInDefaultCorporation())
            {
                if (looter.CorporationEid == owner.CorporationEid)
                    return;
            }

            CheckPinCode(looter.Character,pinCode);
        }

        private bool IsInLootRange(Player player) { return IsInRangeOf3D(player, LOOT_RANGE); }

        private void CheckPinCode(Character looter,int pinCode)
        {
            _pinTryCounts.GetOrDefault(looter).ThrowIfGreaterOrEqual(3, ErrorCodes.AccessDenied);

            if (LootHelper.PinToString(PinCode) != LootHelper.PinToString(pinCode))
            {
                _pinTryCounts.AddOrUpdate(looter, 1, (c, current) => ++current);
                throw new GenxyException(ErrorCodes.AccessDenied);
            }

            _pinTryCounts[looter] = 0;
        }

        private bool IsFieldContainer() { return this is FieldContainer; }

        protected void SendLootListToLooters()
        {
            SendPacketToLooters(_lootListPacketBuilder);
        }

        protected void SendPacketToLooters(IBuilder<Packet> builder)
        {
            _looters.GetLooters().SendPacket(builder);
        }

        public void ReleaseLootContainer(Player player)
        {
            _looters.Remove(player);
        }

        public void TakeLoots(Player player, int pinCode, IList<KeyValuePair<Guid, int>> items)
        {
            HasAccess(player, pinCode);

            lock (syncObject)
            {
                var takeLootBeamBuilder = Beam.NewBuilder().WithType(BeamType.loot_bolt)
                                                           .WithSource(player)
                                                           .WithTarget(this)
                                                           .WithState(BeamState.Hit)
                                                           .WithDuration(TimeSpan.FromSeconds(1));

                Zone.CreateBeam(takeLootBeamBuilder);

                using (var scope = DbQuery.CreateTransaction())
                {
                    var container = player.GetContainer();
                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();
                    var lootedItems = new List<Item>();

                    var progressPacketBuilder = new LootContainerProgressInfoPacketBuilder(container, this, items.Count);

                    foreach (var kvp in items)
                    {
                        try
                        {
                            var lootId = kvp.Key;
                            var reqQty = kvp.Value;

                            var lootItem = _itemRepository.Get(this, lootId);
                            if (lootItem == null)
                                continue;

                            if (lootItem.Quantity < reqQty)
                            {
                                reqQty = lootItem.Quantity;
                            }

                            var item = CreateWithRandomEid(lootItem.ItemInfo);
                            item.Owner = player.Owner;
                            item.Quantity = reqQty;
                            item.IsRepackaged = lootItem.ItemInfo.IsRepackaged;

                            if (!container.IsEnoughCapacity(item))
                                continue;

                            //ha serult akkor legyen serult
                            item.Health = lootItem.ItemInfo.Health;

                            lock (container)
                            {
                                container.AddItem(item, true);
                            }

                            lootItem.Quantity -= reqQty;

                            if (lootItem.Quantity <= 0)
                                _itemRepository.Delete(this,lootItem);
                            else
                                _itemRepository.Update(this,lootItem);

                            lootedItems.Add(item);
                        }
                        finally
                        {
                            SendPacketToLooters(progressPacketBuilder);
                            progressPacketBuilder.Increase();
                        }
                    }

                    Repository.Save(container);

                    Transaction.Current.OnCompleted(c =>
                    {
                        container.SendUpdateToOwnerAsync();

                        OnTakeLoots(player, lootedItems);

                        if (CanRemoveIfEmpty() && _itemRepository.IsEmpty(this))
                        {
                            RemoveFromZone();
                        }
                        else
                        {
                            SendLootListToLooters();
                        }

                        SendPacketToLooters(progressPacketBuilder);
                    });

                    scope.Complete();
                }
            }
        }

        private void OnTakeLoots(Player player, IEnumerable<Item> lootedItems)
        {
            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.TakeLoot).SetCharacter(player.Character).SetContainer(Eid);

            var displayOrder = GetMissionDisplayOrder();
            var missionGuid = GetMissionGuid();

            foreach (var item in lootedItems)
            {
                b.SetItem(item);
                Character.LogTransaction(b);

                if (this is FieldContainer)
                    continue;

#if DEBUG
                Logger.Info(">>>>> ENQUEUE LOOTING >>>>> " + player.Character.Id + " " + item.ED.Name + " qty:" + item.Quantity);
#endif

                player.MissionHandler.EnqueueMissionEventInfo(new LootMissionEventInfo(player,item,CurrentPosition,missionGuid,displayOrder));
                
            }
        }

        protected virtual bool CanRemoveIfEmpty()
        {
            return true;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _looters.Update(time);
            _despawnHelper.Update(time, this);

            if (IsFieldContainer())
                return;

            _timerResetOwner.Update(time);

            if (_timerResetOwner.Passed)
                ResetOwner();
        }

        private void ResetOwner()
        {
            if ( Owner == 0L )
                return;

            DbQuery.CreateTransactionAsync(scope =>
            {
                Owner = 0;
                Repository.Save(this);
                scope.Complete();
            });
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            DbQuery.CreateTransactionAsync(scope =>
            {
                _itemRepository.DeleteAll(this);
                zone.UnitService.RemoveUserUnit(this);
                scope.Complete();
            }).ContinueWith(t => { base.OnRemovedFromZone(zone); });
        }

        protected class LootContainerProgressInfoPacketBuilder : IBuilder<Packet>
        {
            private readonly LootContainer _container;
            private readonly int _maxCount;
            private readonly RobotInventory _robotInventory;
            private int _currentCount;

            public LootContainerProgressInfoPacketBuilder(RobotInventory robotInventory, LootContainer container, int maxCount)
            {
                _container = container;
                _robotInventory = robotInventory;
                _maxCount = maxCount;
            }

            public Packet Build()
            {
                var packet = new Packet(ZoneCommand.LootContainerProgressInfo);

                packet.AppendLong(_robotInventory.Eid);
                packet.AppendLong(_container.Eid);
                packet.AppendInt(_maxCount);
                packet.AppendInt(_currentCount);

                return packet;
            }

            public void Increase()
            {
                _currentCount++;
            }
        }

        private class LootListPacketBuilder : IBuilder<Packet>
        {
            private readonly LootContainer _container;
            private readonly ILootItemRepository _itemRepository;

            public LootListPacketBuilder(LootContainer container, ILootItemRepository itemRepository)
            {
                _container = container;
                _itemRepository = itemRepository;
            }

            public Packet Build()
            {
                var packet = new Packet(ZoneCommand.LootList);

                packet.AppendLong(_container.Eid);

                var loots = _itemRepository.GetAll(_container).ToList();
                packet.AppendInt(loots.Count);

                foreach (var lootItem in loots)
                {
                    lootItem.AppendToPacket(packet);
                }

                return packet;
            }
        }

        private class Looters
        {
            private readonly LootContainer _lootContainer;
            private readonly ConcurrentDictionary<long, Player> _looters = new ConcurrentDictionary<long, Player>();
            private readonly TimerAction _action;

            public Looters(LootContainer lootContainer)
            {
                _lootContainer = lootContainer;
                _action = new TimerAction(CleanUpLooters,TimeSpan.FromSeconds(1000));
            }

            public int Count
            {
                get { return _looters.Count; }
            }

            public IEnumerable<Player> GetLooters()
            {
                return _looters.Values;
            }

            public void Add(Player player)
            {
                _looters[player.Eid] = player;
            }

            public void Remove(Player player)
            {
                _looters.Remove(player.Eid);
            }

            public void Update(TimeSpan time)
            {
                _action.Update(time);
            }

            private void CleanUpLooters()
            {
                foreach (var kvp in _looters)
                {
                    var player = kvp.Value;
                    var isInZone = player.InZone;
                    var isInLootRange = _lootContainer.IsInLootRange(player);

                    if (isInZone && isInLootRange)
                        continue;

                    _looters.Remove(kvp.Key);
                }
            }
        }

        public static LootContainerBuilder Create()
        {
            return new LootContainerBuilder();
        }

        public class LootContainerBuilder
        {
            private static readonly Dictionary<LootContainerType, string> _containerTypeToName = new Dictionary<LootContainerType, string>
            {
                {LootContainerType.LootOnly,DefinitionNames.LOOT_CONTAINER_OBJECT},
                {LootContainerType.Field,DefinitionNames.FIELD_CONTAINER},
                {LootContainerType.Mission,DefinitionNames.MISSION_CONTAINER}
            };

            private readonly List<LootItem> _lootItems = new List<LootItem>();

            private LootContainerType _containerType;
            private Player _ownerPlayer;
            private int _pinCode;
            private BeamType _enterBeamType;

            internal LootContainerBuilder()
            {
                _containerType = LootContainerType.LootOnly;
                _pinCode = FastRandom.NextInt(1, 9999);
                _enterBeamType = BeamType.undefined;
            }

            public LootContainerBuilder SetType(LootContainerType type)
            {
                _containerType = type;
                return this;
            }

            public LootContainerBuilder SetOwner(Player player)
            {
                _ownerPlayer = player;
                return this;
            }

            public LootContainerBuilder SetPinCode(int pinCode)
            {
                _pinCode = pinCode;
                return this;
            }

            public LootContainerBuilder SetEnterBeamType(BeamType beamType)
            {
                _enterBeamType = beamType;
                return this;
            }

            public LootContainerBuilder AddLoot(int definition, int quantity)
            {
                return AddLoot(LootItemBuilder.Create(definition).SetQuantity(quantity));
            }

            public LootContainerBuilder AddLoot(IBuilder<LootItem> builder)
            {
                AddLoot(builder.Build());
                return this;
            }

            public LootContainerBuilder AddLoot(ILootGenerator lootGenerator)
            {
                AddLoot(lootGenerator.Generate());
                return this;
            }

            public LootContainerBuilder AddLoot(IEnumerable<LootItem> lootItems)
            {
                _lootItems.AddRange(lootItems);
                return this;
            }

            public LootContainerBuilder AddLoot(LootItem lootItem)
            {
                _lootItems.Add(lootItem);
                return this;
            }

            [CanBeNull]
            public LootContainer BuildAndAddToZone(IZone zone, Position position)
            {
                if (_lootItems.Count == 0)
                    return null;

                var container = Build(zone, position);
                if (container == null)
                    return null;

                Transaction.Current.OnCommited(() =>
                {
                    var beamBuilder = Beam.NewBuilder().WithType(_enterBeamType).WithSource(_ownerPlayer)
                        .WithTarget(container)
                        .WithState(BeamState.Hit)
                        .WithDuration(TimeSpan.FromSeconds(5));

                    container.AddToZone(zone,position,ZoneEnterType.Default, beamBuilder);
                });

                return container;
            }

            [CanBeNull]
            public LootContainer Build(IZone zone, Position position)
            {
                var definitionName = _containerTypeToName.GetOrDefault(_containerType);
                var container = (LootContainer)CreateUnitWithRandomEID(definitionName);
                if (container == null)
                    return null;

                container.PinCode = _pinCode;

                if (_ownerPlayer != null)
                    container.Owner = _ownerPlayer.Owner;

                container.Initialize();

                container.AddLoots(_lootItems.Where(l => !l.ItemInfo.IsRepackaged));

                var stackedLoots = _lootItems.Where(l => l.ItemInfo.IsRepackaged)
                                        .GroupBy(l => l.ItemInfo.Definition)
                                        .Select(grp => LootItemBuilder.Create(grp.Key).AsRepackaged().SetQuantity(grp.Sum(l => l.Quantity)).Build());

                container.AddLoots(stackedLoots);
                zone.UnitService.AddUserUnit(container,position);
                return container;
            }
        }
    }
}