using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.Missions;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    public class MissionTargetRewardCalculator : MissionTargetVisitor
    {
        private readonly MissionInProgress _missionInProgress;
        private readonly MissionTargetInProgress _targetInProgress;
        private readonly bool _estimation;
        private double _reward;

        private static MissionDataCache _missionDataCache;

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public MissionTargetRewardCalculator(MissionInProgress missionInProgress, MissionTargetInProgress targetInProgress, bool estimation)
        {
            _missionInProgress = missionInProgress;
            _targetInProgress = targetInProgress;
            _estimation = estimation;
            
        }

        private MissionTarget Target
        {
            get { return _targetInProgress.myTarget; }
        }

        private void Log(string message)
        {
            Target.Log(message);
        }

        public double CalculateReward(MissionTarget target)
        {
            target.AcceptVisitor(this);
            return _reward;
        }

      

        private int GetQuantityOrProgress
        {
            get { return _estimation ? _targetInProgress.myTarget.Quantity : _targetInProgress.progressCount; }
        }


        private void PayAsMineral()
        {
            int oneCycle;
            if (!_missionDataCache.GetAmountPerCycleByMineralDefinition(Target.Definition, out oneCycle))
            {
                Logger.Error("mineral was not found " + Target);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            var cycles = GetQuantityOrProgress / oneCycle;

            _reward = cycles * Target.Reward;

            Log("paid as mineral " + _reward + " for " + Target);

        }

        //ECONOMY!  TODO put this in DB!
        public static readonly double[] PayOutMultipliers = new double[] { 100, 175, 250, 350, 475, 600, 700, 800, 900, 1000 };

        private void PayReward()
        {
            var level = _missionInProgress.MissionLevel;
            var pMult = PayOutMultipliers[level];
            var reward = Target.Reward;
            _reward = reward * pMult;
            
            Log("est:" + _estimation + " lvl:" + level + " bRwe:" + reward + " pMult:" + pMult + " SUM:" + _reward + " for " + Target);
            
        }

        private void PayAsArtifact()
        {
            var dangerFee = 0.0;
            if (Target.FindArtifactSpawnsNpcs)
            {
                //artifact quantity is a freak, dont bealieve it!
                var quantity = _targetInProgress.myTarget.Quantity;
                var level = _missionInProgress.MissionLevel;
                var reward = Target.Reward;
                var pMult = PayOutMultipliers[level];
                
                dangerFee = pMult * quantity * reward; 

                Log("artifact danger fee " + _reward + " for " + Target);
                
            }

            PayReward();

            var artifactPure = _reward;
            _reward += dangerFee;

            Log("est:" + _estimation + " dangerFee:" + dangerFee + " artifactPure:" + artifactPure + " SUM:" + _reward + " for " + Target);

        }

      

        private void PayQuantityAndReward()
        {
            var quantity = GetQuantityOrProgress;
            var level = _missionInProgress.MissionLevel;
            var reward = Target.Reward;
            var pMult = PayOutMultipliers[level];

            _reward = pMult * quantity * reward;
            
            Log( "est:" + _estimation + " Q:" + quantity + " lvl:" + level + " bRwe:" + reward + " pMult:" + pMult + " SUM:" + _reward + " for " + Target);

        }


        public override void Visit_MissionTarget(MissionTarget missionTarget)
        {
            
        }

        public override void Visit_RandomMissionTarget(RandomMissionTarget randomMissionTarget)
        {
           
        }
        
        
        public override void Visit_MissionTarget_RND_loot_definition(LootRandomTarget lootRandomTarget)
        {
           PayReward();
        }

        public override void Visit_MissionTarget_RND_pop_npc(PopNpcRandomTarget popNpcRandomTarget)
        {
            PayQuantityAndReward();
        }

        public override void Visit_MissionTarget_RND_kill_definition(KillRandomTarget killRandomTarget)
        {
            PayQuantityAndReward();
        }

        public override void Visit_MissionTarget_RND_lock_unit(LockUnitRandomTarget lockUnitRandomTarget)
        {
            PayQuantityAndReward();
        }
        
        public override void Visit_MissionTarget_RND_use_switch(UseSwitchRandomTarget useSwitchRandomTarget)
        {
            PayReward();
        }

        public override void Visit_MissionTarget_RND_submit_item(SubmitItemRandomTarget submitItemRandomTarget)
        {
           PayReward();

        }

        public override void Visit_MissionTarget_RND_use_itemsupply(ItemSupplyRandomTarget itemSupplyRandomTarget)
        {
            PayReward();
        }

        public override void Visit_MissionTarget_RND_find_artifact(FindArtifactRandomTarget findArtifactRandomTarget)
        {
            PayAsArtifact();
        }

        public override void Visit_MissionTarget_RND_scan_mineral(ScanMineralRandomTarget scanMineralRandomTarget)
        {
           PayReward();
        }
        
        public override void Visit_MissionTarget_RND_drill_mineral(DrillMineralRandomTarget drillMineralRandomTarget)
        {
            PayAsMineral();
        }

        public override void Visit_MissionTarget_RND_harvest_plant(HarvestPlantRandomTarget harvestPlantRandomTarget)
        {
            PayAsMineral();
        }
        
        public override void Visit_MissionTarget_RND_massproduce(MassproduceRandomTarget massproduceRandomTarget)
        {
            PayReward();

        }
        
        public override void Visit_MissionTarget_RND_research(ResearchRandomTarget researchRandomTarget)
        {
           PayReward();
        }

        
    }
}
