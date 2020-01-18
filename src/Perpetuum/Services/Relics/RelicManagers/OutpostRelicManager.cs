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
            bool passableHandler(int x, int y)
            {
                return Zone.IsWalkable(new Point(x, y));
            }
            PathFinder pathFinder = new AStarFinder(Heuristic.None, passableHandler);
            Position outpostPosition = _outpost.CurrentPosition;
            Position invalidPoint = new Position(0, 0);

            Point[] result = null;
            var attemptCount = 0;
            for(attemptCount = 0; attemptCount < 10; attemptCount++)
            {
                var randomPos = _outpost.CurrentPosition.GetRandomPositionInRange2D(90, 350);
                var posFinder = new ClosestWalkablePositionFinder(_zone, randomPos);

                var foundValidLocation = posFinder.Find(out p);

                if (!foundValidLocation)
                {
                    Logger.Info("Invalid location!");
                    return p;
                }

                Logger.Info("Looking for a relic");
                result = pathFinder.FindPath(outpostPosition, p);
                if(result != null)
                {
                    break;
                }
            }

            if (result == null)
            {
                Logger.Info("Invalid location!");
                p = invalidPoint;
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
