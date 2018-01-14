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

    public class RiftManager
    {
        private readonly IZone _zone;
        private readonly TimeRange _spawnTime;
        private readonly RiftSpawnPositionFinder _spawnPositionFinder;
        private readonly IEntityServices _entityServices;
        private bool StrongHoldRiftGenerated { get; set; } = false;
        private Random r;

        private readonly LinkedList<TimeTracker> _nextRiftSpawns = new LinkedList<TimeTracker>();

        public RiftManager(IZone zone,TimeRange spawnTime,RiftSpawnPositionFinder spawnPositionFinder,IEntityServices entityServices)
        {
            _zone = zone;
            _spawnTime = spawnTime;
            _spawnPositionFinder = spawnPositionFinder;
            _entityServices = entityServices;
            r = new Random();
        }

        private int _riftCounts;

        public void Update(TimeSpan time)
        {
            // FIXME: this needs to be in the database.
            // do not spawn rifts on zone 16 (the stronghold)
            while (_riftCounts < 10 && _zone.Id != 16)
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
            rift.RemovedFromZone += OnRiftRemovedFromZone;

            // only generate one stronghold teleport and make it random chance.
            int rand = r.Next(0, 10);
            if (rand == 2 && !StrongHoldRiftGenerated)
            {
                rift.IsDestinationStronghold = true;
            }

            var spawnPosition = _spawnPositionFinder.FindSpawnPosition().ToPosition();
            rift.AddToZone(_zone, spawnPosition, ZoneEnterType.NpcSpawn);
            Logger.Info("Rift spawned. " + rift.ED.Name + " (" + rift.CurrentPosition + ") Stronghold:" + rift.IsDestinationStronghold);
        }

        private void OnRiftRemovedFromZone(Unit unit)
        {
            Interlocked.Decrement(ref _riftCounts);
        }
    }
}