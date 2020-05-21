using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Threading;
using Perpetuum.Units;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Services.Relics
{

    public class AbstractRelic : Unit, IRelic
    {
        protected RelicInfo _info;
        protected IZone _zone;
        protected bool _alive;

        protected const double ACTIVATION_RANGE = 3; //30m
        protected TimeSpan lifespan = TimeSpan.Zero;
        protected virtual TimeSpan MAXLIFESPAN { get { return TimeSpan.FromDays(3); } }

        // Locks - Be sure to use locks independently and do not nest calls into other locks!
        // Lock for Live/Dead state of Relic
        private ReaderWriterLockSlim _lock;
        // Lock for altering and reading the time left in this relic's lifepsan
        private ReaderWriterLockSlim _lifespanLock;
        private readonly TimeSpan THREAD_TIMEOUT = TimeSpan.FromSeconds(4);

        protected RelicLootItems _loots;

        [CanBeNull]
        public static IRelic BuildAndAddToZone(RelicInfo info, IZone zone, Position position, RelicLootItems lootItems)
        {
            var relic = (AbstractRelic)CreateUnitWithRandomEID(DefinitionNames.RELIC);
            if (relic == null)
                return null;
            relic.Init(info, zone, position, lootItems);
            relic.AddToZone(zone, position);
            return relic;
        }

        public void Init(RelicInfo info, IZone zone, Position position, RelicLootItems lootItems)
        {
            _lock = new ReaderWriterLockSlim();
            _lifespanLock = new ReaderWriterLockSlim();
            _info = info;
            _zone = zone;
            CurrentPosition = _zone.GetPosition(position);
            SetAlive(true);
            SetLoots(lootItems);
        }

        public void SetLoots(RelicLootItems lootItems)
        {
            _loots = lootItems;
        }

        public RelicInfo GetRelicInfo()
        {
            return _info;
        }

        public Position GetPosition()
        {
            return CurrentPosition;
        }

        public void SetAlive(bool isAlive)
        {
            using (_lock.Write(THREAD_TIMEOUT))
                _alive = isAlive;
        }

        public bool IsAlive()
        {
            using (_lock.Read(THREAD_TIMEOUT))
                return _alive;
        }

        private void incrementLifeSpan(TimeSpan time)
        {
            using (_lifespanLock.Write(THREAD_TIMEOUT))
                lifespan += time;
        }

        private bool isLifeSpanExpired()
        {
            using (_lifespanLock.Read(THREAD_TIMEOUT))
                return lifespan > MAXLIFESPAN;
        }

        private TimeSpan GetLifeSpan()
        {
            using (_lifespanLock.Read(THREAD_TIMEOUT))
                return lifespan;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            incrementLifeSpan(time);
            if (isLifeSpanExpired())
                SetAlive(false);
            base.OnUpdate(time);
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            SetAlive(false);
            base.OnRemovedFromZone(zone);
        }

        public void RemoveFromZone()
        {
            base.RemoveFromZone();
        }


        protected internal override void UpdatePlayerVisibility(Player player)
        {
            if (GetPosition().TotalDistance2D(player.CurrentPosition) < ACTIVATION_RANGE && IsAlive())
            {
                PopRelic(player);
            }
        }

        public virtual void PopRelic(Player player)
        {
            //Set flag on relic for removal
            SetAlive(false);

            //Compute loots
            if (_loots == null)
                return;

            //Compute EP
            var ep = GetRelicInfo().GetEP();
            if (_zone.Configuration.Type == ZoneType.Pvp) ep *= 2;
            if (_zone.Configuration.Type == ZoneType.Training) ep = 0;

            //Fork task to make the lootcan and log the ep
            Task.Run(() =>
            {
                using (var scope = Db.CreateTransaction())
                {
                    LootContainer.Create().SetOwner(player).SetEnterBeamType(BeamType.loot_bolt).AddLoot(_loots.LootItems).BuildAndAddToZone(_zone, CurrentPosition);
                    if (ep > 0) player.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Artifact, ep);
                    scope.Complete();
                }
            });
        }

        public Dictionary<string, object> ToDebugDictionary()
        {
            var lifeLeft = MAXLIFESPAN - GetLifeSpan();
            var dictionary = new Dictionary<string, object>
            {
                {k.position, this.GetPosition() },
                {k.timeLeft, lifeLeft },
                {"relicinfo", this.GetRelicInfo().ToDictionary().ToDebugString()},
            };

            return dictionary;
        }
    }

}
