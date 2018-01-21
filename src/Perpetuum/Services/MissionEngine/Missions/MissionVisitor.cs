using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.Services.MissionEngine.Missions
{
    public abstract class MissionVisitor
    {
        [DebuggerStepThrough]
        public virtual void VisitMission(Mission mission) { }


        [DebuggerStepThrough]
        public virtual void VisitRandomMission(RandomMission randomMission) { VisitMission(randomMission); }

    }

    public class MissionRewardCalculator : MissionVisitor
    {
        private double _riskCompensation;
        private double _rewardSum;
        private double _distanceReward;
        private double _difficultyReward;
        private double _rewardByTargets;
        private readonly MissionInProgress _missionInProgress;
        private readonly bool _estimation;
        
        public MissionRewardCalculator(MissionInProgress missionInProgress, bool estimation)
        {
            _missionInProgress = missionInProgress;
            _estimation = estimation;
        }

        public override void VisitMission(Mission mission)
        {
            _rewardSum = mission.rewardFee;

        }

        public override void VisitRandomMission(RandomMission randomMission)
        {
            if (_estimation)
            {
                CalculateEstimation();
            }
            else
            {
                CalculateFinalReward();
            }

        }

        private void CalculateEstimation( )
        {
            _difficultyReward = _missionInProgress.GetDifficultyReward;
            _rewardByTargets = _missionInProgress.EstimateRewardByTargets();
            var netReward = _rewardByTargets + _difficultyReward;
            
            _rewardSum = netReward * ZoneFactor;

            Logger.Info("Estimated reward is:" + _rewardSum + " byTargets:" + _rewardByTargets + " difficultyReward:" + _difficultyReward + " for " + _missionInProgress);
        }

        private double ZoneFactor
        {
            get { return _missionInProgress.myLocation.ZoneConfig.IsAlpha ? 1 : 3; }
        }


        private void CalculateFinalReward( )
        {
            _distanceReward = _missionInProgress.CollectDistanceReward();

            _difficultyReward = _missionInProgress.GetDifficultyReward;
            _rewardByTargets = _missionInProgress.CollectRewardByTargets();
            var netReward = _distanceReward + _rewardByTargets + _difficultyReward;
            
            _rewardSum = netReward * ZoneFactor;
            _riskCompensation = _rewardSum - netReward;

            if (MissionResolveTester.isTestMode) return;

            Logger.Info("mission reward:" + _rewardSum + " byTargets:" + _rewardByTargets + " distanceReward:" + _distanceReward + " difficultyReward:" + _difficultyReward + " for " + _missionInProgress);
        }

        public double CalculateReward(Mission mission)
        {
            mission.AcceptVisitor(this);
            return _rewardSum;

        }

        public void CalculateAllRewards(Mission mission, out double rewardSum, out double distanceReward, out double difficultyReward, out double rewardByTargets, out double riskCompensation, out double zoneFactor)
        {
            mission.AcceptVisitor(this);
            rewardSum = _rewardSum;
            distanceReward = _distanceReward;
            difficultyReward = _difficultyReward;
            rewardByTargets = _rewardByTargets;
            riskCompensation = _riskCompensation;
            zoneFactor = ZoneFactor;
        }
    }

    public class MissionStandingChangeCalculator : MissionVisitor
    {
        private IEnumerable<MissionStandingChange> _standingChanges;
        private readonly MissionLocation _missionLocation;
        private readonly MissionInProgress _missionInProgress;

        private static MissionDataCache _missionDataCache;

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public MissionStandingChangeCalculator(MissionInProgress missionInProgress,  MissionLocation missionLocation)
        {
            _missionLocation = missionLocation;
            _missionInProgress = missionInProgress;
        }

        public IEnumerable<MissionStandingChange> CollectStandingChanges(Mission mission)
        {
            mission.AcceptVisitor(this);
            return _standingChanges;
        }


        public override void VisitMission(Mission mission)
        {
            _standingChanges = mission.StandingChanges;
        }

        /// <summary>
        /// Ebben van a ko papir ollos cucc, 1hez ad 1tol levon
        /// </summary>
        /// <param name="randomMission"></param>
        public override void VisitRandomMission(RandomMission randomMission)
        {
            var missionLevel = _missionInProgress.MissionLevel;

            var rawGrindLevel = _missionDataCache.LookUpGrindAmount(missionLevel);

            var diffmult = randomMission.DifficultyMultiplier;

            var standingValue = 1 / (diffmult * rawGrindLevel);

            // 0 -> 0.0   10 - > 1.0
            // 6 -> 0.6 --> 0.5*0.6 => positive x 30%
 
            var f = _missionInProgress.MissionLevel.Clamp(0, 9) / 9.0 ; 
            
            var negativeValue = -1 * (standingValue / 2) * f; //20% extension can reduce it to 10%

            var positiveAllianceEid = _missionLocation.Agent.OwnerAlliance.Eid;
            var opposingAllianceEid = DefaultCorporationDataCache.SelectOpposingAlliance(positiveAllianceEid);


            //TODO: Fixme: I am a hack for Syndicatification
            if (this._missionLocation.zoneId == 0 || this._missionLocation.zoneId == 8) //For New Virginia and Hershfield
            {
                //Implementation: Add reputation for all factions equally -- No factional bias!
                IEnumerable<long> allianceEids = DefaultCorporationDataCache.GetMegaCorporationEids();
                List<MissionStandingChange> standingChangeForSyndicatification = new List<MissionStandingChange>();
                foreach(long id in allianceEids)
                {
                    standingChangeForSyndicatification.Add(new MissionStandingChange(id, standingValue));
                }
                _standingChanges = standingChangeForSyndicatification.ToArray();
            }//--end hack--
            else
            {
                _standingChanges = new[] { new MissionStandingChange(positiveAllianceEid, standingValue), new MissionStandingChange(opposingAllianceEid, negativeValue) };
            }
        }

    }

    public class MissionRewardItemSelector : MissionVisitor
    {
        private readonly MissionInProgress _missionInProgress;
        private IEnumerable<MissionReward> _rewardItems;

        public MissionRewardItemSelector(MissionInProgress missionInProgress)
        {
            _missionInProgress = missionInProgress;
        }


        public override void VisitMission(Mission mission)
        {
            _rewardItems = mission.RewardItems;
        }

        public override void VisitRandomMission(RandomMission randomMission)
        {
            var coinDefinition = _missionInProgress.myLocation.GetRaceSpecificCoinDefinition();

            var level = _missionInProgress.MissionLevel;

            //var coinQuantity = (int)Math.Round(Math.Pow(1.3 + level, 2.5 / 3.0) * randomMission.DifficultyMultiplier);
            var coinQuantity = RandomMission.CoinQuantity(level, randomMission.DifficultyMultiplier);
            
            var coinItemInfo = new ItemInfo(coinDefinition, coinQuantity);
            var coinReward = new MissionReward(coinItemInfo);

            var rewardItems = randomMission.RewardItems.ToList();
            rewardItems.Add(coinReward);
            _rewardItems = rewardItems;

        }

        public IEnumerable<MissionReward> SelectRewards(Mission mission)
        {
            mission.AcceptVisitor(this);
            return _rewardItems;
        }

    }
   
}
