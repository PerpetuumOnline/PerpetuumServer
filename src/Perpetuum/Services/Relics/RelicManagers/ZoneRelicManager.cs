using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Services.RiftSystem;
using System.Drawing;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Beams;
using System.Threading;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.Relics
{
    public class ZoneRelicManager : AbstractRelicManager
    {
        //Spawn time params
        private readonly TimeSpan RESPAWN_RANDOM_WINDOW = TimeSpan.FromHours(1);
        private readonly TimeSpan _respawnRate = TimeSpan.FromHours(1.5);

        private Random _random;

        private IEnumerable<RelicSpawnInfo> _spawnInfos;

        //Beam Draw refresh
        private readonly TimeSpan _relicRefreshRate = TimeSpan.FromSeconds(19.95);

        //DB-accessing objects
        private readonly RelicZoneConfigRepository relicZoneConfigRepository;
        private readonly RelicSpawnInfoRepository relicSpawnInfoRepository;

        //Child RelicManagers
        private IList<OutpostRelicManager> outpostRelicManagers = new List<OutpostRelicManager>();

        private RiftSpawnPositionFinder _spawnPosFinder;

        private IZone _zone;
        protected override IZone Zone
        {
            get
            {
                return _zone;
            }
        }
        private ReaderWriterLockSlim _lock;
        protected override ReaderWriterLockSlim Lock
        {
            get
            {
                return _lock;
            }
        }

        public ZoneRelicManager(IZone zone)
        {
            _lock = new ReaderWriterLockSlim();
            _random = new Random();
            _relics = new List<IRelic>();
            _zone = zone;
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

        public override void Start()
        {
            base.Start();
            var outposts = _zone.Units.OfType<Outpost>().ToList();
            foreach (var outpost in outposts)
            {
                outpostRelicManagers.Add(new OutpostRelicManager(outpost));
            }
            foreach (var childManagers in outpostRelicManagers)
            {
                childManagers.Start();
            }
        }

        public override void Stop()
        {
            foreach (var childManagers in outpostRelicManagers)
            {
                childManagers.Stop();
            }
            base.Stop();
        }

        public override void Update(TimeSpan time)
        {
            base.Update(time);
            foreach (var childManagers in outpostRelicManagers)
            {
                childManagers.Update(time);
            }
        }

        protected override IRelic MakeRelic(RelicInfo info, Position position)
        {
            return Relic.BuildAndAddToZone(info, _zone, position, relicLootGenerator.GenerateLoot(info));
        }

        protected override TimeSpan RollNextSpawnTime()
        {
            var randomFactor = _random.NextDouble() - 0.5;
            var minutesToAdd = RESPAWN_RANDOM_WINDOW.TotalMinutes * randomFactor;

            return _respawnRate.Add(TimeSpan.FromMinutes(minutesToAdd));
        }


        protected override RelicInfo GetNextRelicType()
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

        protected override Point FindRelicPosition(RelicInfo info)
        {
            if (info.HasStaticPosistion) //If the relic spawn info has a valid static position defined - use that
            {
                return info.GetPosition().ToPoint();
            }
            return _spawnPosFinder.FindSpawnPosition(); //Else use random-walkable
        }

        protected override List<Dictionary<string, object>> DoGetRelicListDictionary()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var childManagers in outpostRelicManagers)
            {
                list.AddMany(childManagers.GetRelicListDictionary());
            }
            list.AddMany(_relics.Select(r => r.ToDebugDictionary()).ToList());
            return list;
        }

        protected override void RefreshBeam(IRelic relic)
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
    }
}
