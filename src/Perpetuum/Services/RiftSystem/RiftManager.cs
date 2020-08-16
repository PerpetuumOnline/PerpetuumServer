using System;
using System.Collections.Generic;
using System.Drawing;
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

    public class RiftManager : IRiftManager
    {
        private readonly ZoneRiftConfig _config;
        private readonly IZone _zone;
        private readonly TimeRange _spawnTime;
        private readonly RiftSpawnPositionFinder _spawnPositionFinder;
        private readonly IEntityServices _entityServices;

        private readonly LinkedList<TimeTracker> _nextRiftSpawns = new LinkedList<TimeTracker>();

        public RiftManager(IZone zone, TimeRange spawnTime, RiftSpawnPositionFinder spawnPositionFinder, IEntityServices entityServices)
        {
            _zone = zone;
            _config = ZoneRiftConfigReader.GetForZone(zone);
            _spawnTime = spawnTime;
            _spawnPositionFinder = spawnPositionFinder;
            _entityServices = entityServices;
        }

        private int _riftCounts;

        public void Update(TimeSpan time)
        {
            while (_riftCounts < _config.MaxRifts)
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

        private void SpawnRift()
        {
            var rift = (Rift)_entityServices.Factory.CreateWithRandomEID(DefinitionNames.RIFT);

            rift.SetDespawnTime(TimeSpan.FromHours(3));
            rift.SetLevel(_config.MaxLevel);
            rift.RemovedFromZone += OnRiftRemovedFromZone;

            var spawnPosition = _spawnPositionFinder.FindSpawnPosition().ToPosition();

            rift.AddToZone(_zone, spawnPosition, ZoneEnterType.NpcSpawn);
            Logger.Info(string.Format("Rift spawned on zone {0} {1} ({2})", _zone.Id, rift.ED.Name, rift.CurrentPosition));
        }

        private void OnRiftRemovedFromZone(Unit unit)
        {
            Interlocked.Decrement(ref _riftCounts);
        }
    }
}
