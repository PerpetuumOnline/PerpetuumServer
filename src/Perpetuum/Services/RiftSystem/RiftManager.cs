using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Services.RiftSystem
{

    public abstract class RiftSpawnPositionFinder
    {
        private readonly IZone _zone;

        protected RiftSpawnPositionFinder(IZone zone)
        {
            _zone = zone;
        }

        public Point FindSpawnPosition()
        {
            return FindSpawnPosition(_zone);
        }

        protected abstract Point FindSpawnPosition(IZone zone);
    }

    public class PveRiftSpawnPositionFinder : RiftSpawnPositionFinder
    {
        public PveRiftSpawnPositionFinder(IZone zone) : base(zone)
        {
        }

        protected override Point FindSpawnPosition(IZone zone)
        {
            return zone.GetRandomPassablePosition();
        }
    }

    public class PvpRiftSpawnPositionFinder : RiftSpawnPositionFinder
    {
        public PvpRiftSpawnPositionFinder(IZone zone) : base(zone)
        {
        }

        protected override Point FindSpawnPosition(IZone zone)
        {
            var p = zone.FindWalkableArea(zone.Size.ToArea(), 20);
            return p.RandomElement();
        }
    }

    public class RiftManager
    {
        private readonly IZone _zone;
        private readonly TimeRange _spawnTime;
        private readonly RiftSpawnPositionFinder _spawnPositionFinder;
        private readonly IEntityServices _entityServices;
        private readonly ZoneManager _zonemanager;
        private bool StrongHoldRiftGenerated { get; set; } = false;
        private Random r;

        private readonly LinkedList<TimeTracker> _nextRiftSpawns = new LinkedList<TimeTracker>();

        public RiftManager(IZone zone,TimeRange spawnTime,RiftSpawnPositionFinder spawnPositionFinder,IEntityServices entityServices, ZoneManager zoneManager)
        {
            _zone = zone;
            _spawnTime = spawnTime;
            _spawnPositionFinder = spawnPositionFinder;
            _entityServices = entityServices;
            _zonemanager = zoneManager;
            r = new Random();
        }

        private int _riftCounts;

        public void Update(TimeSpan time)
        {
            while (_riftCounts < 10 && !(_zone is StrongHoldZone))
            {
                _nextRiftSpawns.AddLast(new TimeTracker(FastRandom.NextTimeSpan(_spawnTime)));
                Interlocked.Increment(ref _riftCounts);
            }
         
            _nextRiftSpawns.RemoveAll(t =>
            {
                t.Update(time);
                if (!t.Expired)
                    return false;

                SpawnRift();
                return true;
            });
        }

        /// <summary>
        /// Get a random active stronghold zone.
        /// </summary>
        /// <returns>int</returns>
        private int GetRandomStrongHoldZone()
        {
            IEnumerable<Zone> strongholdzones = _zonemanager.Zones.OfType<StrongHoldZone>();
            // less than elegant. if we only have one stronghold zone just return it.
            if (strongholdzones.Count() == 1)
            {
                return strongholdzones.ElementAt(0).Id;
            }
            int index = r.Next(0, strongholdzones.Count());
            Zone selzone = strongholdzones.ElementAt(index);
            return selzone.Id;
        }

        private void SpawnRift()
        {
            var rift = (Rift)_entityServices.Factory.CreateWithRandomEID(DefinitionNames.RIFT);

            rift.SetDespawnTime(TimeSpan.FromHours(3));
            rift.RemovedFromZone += OnRiftRemovedFromZone;

            // generate a stronghold teleport and make it random chance.
            // make sure we have at least one stronghold enabled.
            if (_zonemanager.Zones.OfType<StrongHoldZone>().Count() > 0)
            {
                int rand = r.Next(0, 10);
                if (rand == 2 && !StrongHoldRiftGenerated)
                {
                    rift.DestinationStrongholdZone = GetRandomStrongHoldZone();
                    rift.OriginZone = this._zone.Id;
                }
            }

            var spawnPosition = _spawnPositionFinder.FindSpawnPosition().ToPosition();
            rift.AddToZone(_zone, spawnPosition, ZoneEnterType.NpcSpawn);
            Logger.Info(string.Format("Rift spawned on zone {0} {1} ({2}) Stronghold Zone ID: {3}", _zone.Id, rift.ED.Name, rift.CurrentPosition, rift.DestinationStrongholdZone));
        }

        private void OnRiftRemovedFromZone(Unit unit)
        {
            Interlocked.Decrement(ref _riftCounts);
        }
    }
}