using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Drawing;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.Finders.PositionFinders;
using System.Threading;
using Perpetuum.PathFinders;
using Perpetuum.Accounting.Characters;
using Perpetuum.Units;
using Perpetuum.Log;

namespace Perpetuum.Services.Relics
{
    public class OutpostRelicManager : AbstractRelicManager
    {
        //Spawn time params
        private readonly TimeSpan RESPAWN_RANDOM_WINDOW = TimeSpan.FromHours(1);
        private readonly TimeSpan _respawnRate = TimeSpan.FromHours(4);

        private Outpost _outpost;
        private Random _random;
        private RelicInfo _sapRelicInfo;

        //Beam Draw refresh
        private readonly TimeSpan _relicRefreshRate = TimeSpan.FromSeconds(19.95);

        // Spawn range and area
        private readonly int SPAWN_MINIMUM_OUTPOST_DISTANCE = 90;
        private readonly int SPAWN_MAXIMUM_OUTPOST_DISTANCE = 350;
        private readonly int SPAWN_AREA_REQUIRED_SIZE = 200;

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

        public OutpostRelicManager(Outpost outpost)
        {
            _lock = new ReaderWriterLockSlim();
            _outpost = outpost;
            _random = new Random();
            _relics = new List<IRelic>();
            _zone = outpost.Zone;
            relicLootGenerator = new RelicLootGenerator();
            _max_relics = 1;
            _respawnRandomized = RollNextSpawnTime();
            _sapRelicInfo = RelicInfo.GetByNameFromDB("sap_relic_basetype");
        }

        protected override IRelic MakeRelic(RelicInfo info, Position position)
        {
            return SAPRelic.BuildAndAddToZone(info, _zone, position, relicLootGenerator.GenerateLoot(info), _outpost);
        }

        protected override TimeSpan RollNextSpawnTime()
        {
            var randomFactor = _random.NextDouble() - 0.5;
            var minutesToAdd = RESPAWN_RANDOM_WINDOW.TotalMinutes * randomFactor;
            return _respawnRate.Add(TimeSpan.FromMinutes(minutesToAdd));
        }

        protected override RelicInfo GetNextRelicType()
        {
            return _sapRelicInfo;
        }

        protected override Point FindRelicPosition(RelicInfo info)
        {
            Position p = new Position();
            Position invalidPosition = new Position(0, 0);

            List<Point> result = null;
            for(int i = 0; i < 10; i++)
            {
                var randomPos = _outpost.CurrentPosition.GetRandomPositionInRange2D(SPAWN_MINIMUM_OUTPOST_DISTANCE, SPAWN_MAXIMUM_OUTPOST_DISTANCE);
                var posFinder = new ClosestWalkablePositionFinder(_zone, randomPos);

                posFinder.Find(out p);
                result = _zone.FindWalkableArea(p, _zone.Size.ToArea(), SPAWN_AREA_REQUIRED_SIZE);
                if(result != null)
                {
                    break;
                }
            }

            if (result == null)
            {
                Logger.Info("Invalid location!");
                p = invalidPosition;
            }

            return p;
        }

        protected override void RefreshBeam(IRelic relic)
        {
            var position = relic.GetPosition();
            var p = _zone.FixZ(position);
            var beamBuilder = Beam.NewBuilder().WithType(BeamType.sap_scanner_beam).WithTargetPosition(position)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(60);
            _zone.CreateBeam(beamBuilder);
            beamBuilder = Beam.NewBuilder().WithType(BeamType.nature_effect).WithTargetPosition(position)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(_relicRefreshRate);
            _zone.CreateBeam(beamBuilder);
            beamBuilder = Beam.NewBuilder().WithType(BeamType.green_20sec).WithTargetPosition(p.AddToZ(10))
                .WithState(BeamState.Hit)
                .WithDuration(_relicRefreshRate);
            _zone.CreateBeam(beamBuilder);

        }
    }
}
