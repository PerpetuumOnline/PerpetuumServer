using System.Collections.Generic;
using System.Linq;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.Missions;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    public class MissionTargetSuccessInfoGenerator : MissionTargetVisitor
    {
        private readonly MissionInProgress _missionInProgress;
        private readonly MissionTargetInProgress _missionTargetInProgress;
        private readonly List<Position> _terminalPositions;
        private readonly int _zoneId;

        private static MissionDataCache _missionDataCache;

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public MissionTargetSuccessInfoGenerator(MissionInProgress missionInProgress, MissionTargetInProgress missionTargetInProgress, List<Position> terminalPositions   )
        {
            _missionInProgress = missionInProgress;
            _missionTargetInProgress = missionTargetInProgress;
            _terminalPositions = terminalPositions;
            _zoneId = _missionInProgress.myLocation.ZoneConfig.Id;
        }

        
        /// <summary>
        /// Where my target's x,y got set
        /// </summary>
        private void GenerateFakeInfoByChoosenPosition()
        {
           
            var x = _missionTargetInProgress.myTarget.targetPosition.intX;
            var y = _missionTargetInProgress.myTarget.targetPosition.intY;

            _missionTargetInProgress.SetSuccessInfo(_zoneId, x, y);

        }

        /// <summary>
        /// Where my target's x,y got set + random radius 
        /// </summary>
        private void GenerateFakeInfoForArtifact()
        {

            var x = _missionTargetInProgress.myTarget.targetPosition.intX;
            var y = _missionTargetInProgress.myTarget.targetPosition.intY;

            var p = new Position(x, y).GetRandomPositionInRange2D(new IntRange(0,_missionTargetInProgress.myTarget.TargetPositionRange));
            
            _missionTargetInProgress.SetSuccessInfo(_zoneId, p.intX, p.intY);

        }


        private void PickClosestMissionLocation()
        {
            var p = new Position();

            var minDistance = double.MaxValue;
            var locations = _missionDataCache.GetAllLocations.Where(l => l.ZoneConfig.Id == _missionInProgress.myLocation.ZoneConfig.Id);
            foreach (var location in locations)
            {
                var distance = location.MyPosition.TotalDistance2D(_missionInProgress.SearchOrigin);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    p = location.MyPosition;
                }
            }

            _missionTargetInProgress.SetSuccessInfo(_zoneId, p.intX, p.intY);
        }


        private void PickClosestTerminalForProduction()
        {
            var p = new Position();

            var minDistance = double.MaxValue;

            foreach (var terminalPosition in _terminalPositions)
            {
                var distance = terminalPosition.TotalDistance2D(_missionInProgress.SearchOrigin);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    p = terminalPosition;
                }

            }

            _missionTargetInProgress.SetSuccessInfo(_zoneId, p.intX, p.intY);

        }




        private void GenerateFakeInfoWithSearchOriginAndRandom(int radius =20)
        {
            var x = _missionInProgress.SearchOrigin.intX + FastRandom.NextInt(-1*radius, radius);
            var y = _missionInProgress.SearchOrigin.intY + FastRandom.NextInt(-1*radius, radius);

            _missionTargetInProgress.SetSuccessInfo(_zoneId, x, y);

        }



        private void GenerateFakeInfoForFetch()
        {

            if (_missionTargetInProgress.myTarget.ValidMissionStructureEidSet)
            {
                _missionTargetInProgress.SetSuccessInfo(_missionTargetInProgress.myTarget.ZoneId, _missionTargetInProgress.myTarget.targetPosition.intX, _missionTargetInProgress.myTarget.targetPosition.intY);
            }
            else
            {
                PickClosestMissionLocation();
            }

        }

        private const int MineralFakeDistance = 50;

        private void GenerateFakeInfoUseMissionLocation()
        {
            _missionTargetInProgress.SetSuccessInfo(_missionInProgress.myLocation.ZoneConfig.Id, (int) _missionInProgress.myLocation.X, (int) _missionInProgress.myLocation.Y);
        }


        public override void Visit_MissionTarget_RND_fetch_item(FetchItemRandomTarget fetchItemRandomTarget)
        {
            GenerateFakeInfoForFetch();
        }

        public override void Visit_MissionStructureTarget(MissionStructureTarget missionStructureTarget)
        {
            GenerateFakeInfoByChoosenPosition();
        }

        public override void Visit_MissionTarget_RND_pop_npc(PopNpcRandomTarget popNpcRandomTarget)
        {
            GenerateFakeInfoByChoosenPosition();
        }

        public override void Visit_MissionTarget_RND_drill_mineral(DrillMineralRandomTarget drillMineralRandomTarget)
        {
            GenerateFakeInfoWithSearchOriginAndRandom(MineralFakeDistance);
        }

        public override void Visit_MissionTarget_RND_harvest_plant(HarvestPlantRandomTarget harvestPlantRandomTarget)
        {
            GenerateFakeInfoWithSearchOriginAndRandom(MineralFakeDistance);
        }

        public override void Visit_MissionTarget_RND_find_artifact(FindArtifactRandomTarget findArtifactRandomTarget)
        {
            GenerateFakeInfoForArtifact();
        }

        public override void Visit_MissionTarget_RND_kill_definition(KillRandomTarget killRandomTarget)
        {
            GenerateFakeInfoWithSearchOriginAndRandom();
        }

        public override void Visit_MissionTarget_RND_lock_unit(LockUnitRandomTarget lockUnitRandomTarget)
        {
            GenerateFakeInfoWithSearchOriginAndRandom();
        }

        public override void Visit_MissionTarget_RND_loot_definition(LootRandomTarget lootRandomTarget)
        {
            GenerateFakeInfoWithSearchOriginAndRandom();
        }

        public override void Visit_MissionTarget_RND_scan_mineral(ScanMineralRandomTarget scanMineralRandomTarget)
        {
            GenerateFakeInfoWithSearchOriginAndRandom(MineralFakeDistance);
        }

        public override void Visit_MissionTarget_RND_massproduce(MassproduceRandomTarget massproduceRandomTarget)
        {
            PickClosestTerminalForProduction();
        }

        public override void Visit_MissionTarget_RND_research(ResearchRandomTarget researchRandomTarget)
        {
            PickClosestTerminalForProduction();
        }

    }
}
