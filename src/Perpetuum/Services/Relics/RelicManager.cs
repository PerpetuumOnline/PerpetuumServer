using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.Looting;
using System.Drawing;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Data;
using Perpetuum.Zones.Beams;
using System.Threading;
using Perpetuum.Threading;
using Perpetuum.EntityFramework;
using Perpetuum.Units;
using Perpetuum.Items;

namespace Perpetuum.Services.Relics
{
    public class RelicManager
    {
        //Constants
        private const double ACTIVATION_RANGE = 3; //30m
        private const double RESPAWN_PROXIMITY = 10.0 * ACTIVATION_RANGE;
        private readonly TimeSpan RESPAWN_RANDOM_WINDOW = TimeSpan.FromHours(1);
        private readonly TimeSpan THREAD_TIMEOUT = TimeSpan.FromSeconds(4);

        private IZone _zone;
        private RiftSpawnPositionFinder _spawnPosFinder;
        private ReaderWriterLockSlim _lock;
        private Random _random;

        private int _max_relics = 0;
        private IEnumerable<RelicSpawnInfo> _spawnInfos;
        private List<Relic> _relics;

        private readonly TimeSpan _respawnRate = TimeSpan.FromHours(1.5);
        private readonly TimeSpan _relicRefreshRate = TimeSpan.FromSeconds(19.95);

        //DB-accessing objects
        private RelicZoneConfigRepository relicZoneConfigRepository;
        private RelicSpawnInfoRepository relicSpawnInfoRepository;
        private RelicLootGenerator relicLootGenerator;
        private readonly IEntityServices _entityServices;

        //Timers for update
        private TimeSpan _refreshElapsed;
        private TimeSpan _respawnElapsed;
        private TimeSpan _respawnRandomized;

        public RelicManager(IZone zone, IEntityServices entityServices)
        {
            _random = new Random();
            _lock = new ReaderWriterLockSlim();
            _relics = new List<Relic>();
            _zone = zone;
            _entityServices = entityServices;
            _spawnPosFinder = new PveRiftSpawnPositionFinder(zone);
            if (zone.Configuration.Terraformable)
            {
                _spawnPosFinder = new PvpRiftSpawnPositionFinder(zone);
            }
            // init repositories and extract data
            relicZoneConfigRepository = new RelicZoneConfigRepository(zone);
            relicSpawnInfoRepository = new RelicSpawnInfoRepository(zone);
            relicLootGenerator = new RelicLootGenerator();

            //Get Zone Relic-Configuration data
            var config = relicZoneConfigRepository.GetZoneConfig();
            _max_relics = config.GetMax();
            _respawnRate = config.GetTimeSpan();
            _respawnRandomized = RollNextSpawnTime();

            _spawnInfos = relicSpawnInfoRepository.GetAll();
        }

        private TimeSpan RollNextSpawnTime()
        {
            var randomFactor = _random.NextDouble() - 0.5;
            var minutesToAdd = RESPAWN_RANDOM_WINDOW.TotalMinutes * randomFactor;

            return _respawnRate.Add(TimeSpan.FromMinutes(minutesToAdd));
        }

        private int GetRelicCount()
        {
            using (_lock.Read(THREAD_TIMEOUT))
                return _relics.Count;
        }

        public void Start()
        {
            //Inject max relics on first start
            while (GetRelicCount() < _max_relics)
            {
                SpawnRelic();
            }
        }

        public void Stop()
        {
            //TODO cleanup if using DB to cache relics
        }

        public bool ForceSpawnRelicAt(int x, int y)
        {
            bool success = false;
            try
            {
                var info = GetNextRelicType();
                if (info == null)
                {
                    return false;
                }
                Position position = new Position(x, y);
                AddRelicToZone(info, position);
                success = true;
            }
            catch (Exception e)
            {
                Logger.Warning("Failed to spawn Relic by ForceSpawnRelicAt()");
                Logger.Warning(e.Message);
            }
            return success;
        }

        public List<Dictionary<string, object>> GetRelicListDictionary()
        {
            using (_lock.Read(THREAD_TIMEOUT))
                return _relics.Select(r => r.ToDebugDictionary()).ToList();
        }

        private Point FindRelicPosition(RelicInfo info)
        {
            if (info.HasStaticPosistion) //If the relic spawn info has a valid static position defined - use that
            {
                return info.GetPosition().ToPoint();
            }
            return _spawnPosFinder.FindSpawnPosition(); //Else use random-walkable
        }

        private RelicInfo GetNextRelicType()
        {
            var spawnRates = _spawnInfos;
            double sumRate = spawnRates.Sum(r => r.GetRate());
            double minRate = 0.0;
            double chance = _random.NextDouble();
            RelicInfo info = null;
            foreach (var spawnRate in spawnRates)
            {
                double rate = (double)spawnRate.GetRate() / sumRate;
                double maxRate = rate + minRate;

                if (minRate < chance && chance <= maxRate)
                {
                    info = spawnRate.GetRelicInfo();
                    break;
                }
                minRate += rate;
            }
            return info;
        }


        private void SpawnRelic()
        {
            //Get Next Relictype based on the distribution of their probabilities on this zone
            var maxAttempts = 100;
            var attempts = 0;
            RelicInfo info = null;
            while (info == null)
            {
                info = GetNextRelicType();
                if (info.HasStaticPosistion && IsSpawnTooClose(info.GetPosition())) //The selected Relic type is static!  We must check if another relic is in this location
                {
                    info = null;
                }
                attempts++;
                if (attempts > maxAttempts)
                {
                    Logger.Error("Could not get RelicInfo for next Relic on Zone: " + _zone.Id);
                    return;
                }
            }

            attempts = 0;
            Point pt = FindRelicPosition(info);
            while (IsSpawnTooClose(pt))
            {
                pt = _spawnPosFinder.FindSpawnPosition();
                attempts++;
                if (attempts > maxAttempts)
                {
                    Logger.Error("Could not get Position for next Relic on Zone: " + _zone.Id);
                    return;
                }
            }
            AddRelicToZone(info, pt.ToPosition());
        }

        private void AddRelicToZone(RelicInfo info, Position position)
        {
            using (_lock.Write(THREAD_TIMEOUT))
            {
                var r = Relic.BuildAndAddToZone(info, _zone, position, relicLootGenerator.GenerateLoot(info));
                if (r != null)
                {
                    _relics.Add(r);
                }
            }
        }

        private bool IsSpawnTooClose(Point point)
        {
            using (_lock.Read(THREAD_TIMEOUT))
            {
                foreach (var r in _relics)
                {
                    if (RESPAWN_PROXIMITY > point.Distance(r.GetPosition()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void RefreshBeam(Relic relic)
        {
            var info = relic.GetRelicInfo();
            var level = info.GetLevel();
            var faction = info.GetFaction();
            var position = relic.GetPosition();
            var factionalBeamType = BeamType.orange_20sec;
            switch (faction)
            {
                case 0:
                    factionalBeamType = BeamType.orange_20sec;
                    break;
                case 1:
                    factionalBeamType = BeamType.green_20sec;
                    break;
                case 2:
                    factionalBeamType = BeamType.blue_20sec;
                    break;
                case 3:
                    factionalBeamType = BeamType.red_20sec;
                    break;
                default:
                    factionalBeamType = BeamType.orange_20sec;
                    break;
            }

            var p = _zone.FixZ(position);
            var beamBuilder = Beam.NewBuilder().WithType(BeamType.artifact_radar).WithTargetPosition(position)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(_relicRefreshRate);
            _zone.CreateBeam(beamBuilder);
            beamBuilder = Beam.NewBuilder().WithType(BeamType.nature_effect).WithTargetPosition(position)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(_relicRefreshRate);
            _zone.CreateBeam(beamBuilder);
            for (var i = 0; i < level; i++)
            {
                beamBuilder = Beam.NewBuilder().WithType(factionalBeamType).WithTargetPosition(p.AddToZ(3.5 * i + 1.0))
                    .WithState(BeamState.Hit)
                    .WithDuration(_relicRefreshRate);
                _zone.CreateBeam(beamBuilder);
            }
        }

        private void UpdateRelics()
        {
            using (_lock.Write(THREAD_TIMEOUT))
            {
                foreach (Relic r in _relics)
                {
                    if (!r.IsAlive())
                    {
                        r.RemoveFromZone();
                    }
                }
                _relics.RemoveAll(r => !r.IsAlive());
            }
            using (_lock.Read(THREAD_TIMEOUT))
            {
                foreach (Relic r in _relics)
                {
                    RefreshBeam(r);
                }
            }
        }

        public void Update(TimeSpan time)
        {
            //Minimum tick rate
            _refreshElapsed += time;
            if (_refreshElapsed < _relicRefreshRate)
                return;

            //Update Relic lifespans, refresh beams and remove dead relics
            UpdateRelics();

            _respawnElapsed += _refreshElapsed;
            _refreshElapsed = TimeSpan.Zero;

            //check if time to spawn a new Relic
            if (_respawnElapsed > _respawnRandomized)
            {
                if (GetRelicCount() < _max_relics)
                {
                    SpawnRelic();
                    _respawnRandomized = RollNextSpawnTime();
                }
                _respawnElapsed = TimeSpan.Zero;
            }

        }
    }
}
