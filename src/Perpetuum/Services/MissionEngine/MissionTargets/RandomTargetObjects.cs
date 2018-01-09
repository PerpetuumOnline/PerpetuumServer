using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Zones.Scanning;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    /// <summary>
    ///     dummy target type to mark a dynamic location
    /// </summary>
    [Serializable]
    public class RandomPointMissionTarget : MissionTarget
    {
        public RandomPointMissionTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_rnd_point(this);
        }
    }


    [Serializable]
    public abstract class MissionStructureTarget : RandomMissionTarget
    {
        protected MissionStructureTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionStructureTarget(this);
        }


        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            if (!ValidDefinitionSet)
            {
                LookupPrimaryDefinitionOrSetFromPools(missionInProgress);
            }

            base.ResolveLinks(missionInProgress);
        }

        public override bool ResolveLocation(MissionInProgress missionInProgress)
        {
            if (ResolveMissionStructureLocations(missionInProgress, Type))
            {
                return base.ResolveLocation(missionInProgress);
            }

            return false;
        }


        public bool ResolveMissionStructureLocations(MissionInProgress missionInProgress, MissionTargetType targetType)
        {
            var selectedTarget = SelectRandomMissionStructure(missionInProgress, targetType);

            //search failed
            if (selectedTarget == null) return false;

            missionStructureEid = selectedTarget.MissionStructureEid;

            CopyZoneInfo(selectedTarget);

            missionInProgress.SearchOrigin = new Position(targetPositionX ?? 0, targetPositionY ?? 0);

            SetTargetPosition_RandomTarget();

            return true;
        }
    }


    [Serializable]
    public class PopNpcRandomTarget : RandomMissionTarget
    {
        public PopNpcRandomTarget(IDataRecord record) : base(record)
        {
            useQuantityOnly = true; //this is forcing the new tech
        }

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_pop_npc(this);
        }


        public override MissionTargetType Type
        {
            get { return MissionTargetType.pop_npc; }
        }

        public override bool ResolveLocation(MissionInProgress missionInProgress)
        {
            if (ResolveGenericLocation(missionInProgress))
            {
                return base.ResolveLocation(missionInProgress);
            }
            return false;
        }


        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            //ez persze csak extra, de lehet, talan kelleni fog
            //tulajodonkeppen sosem lesz a presence hasznalva
            if (!ValidPresenceSet)
            {
                //ha nincs beallitva neki fixen presence, akkor 
                //link resolve, if link set
                //generate definition from pool
                LookUpPrimaryDefinitionOrSelectNpcDefinition(missionInProgress);
            }

            base.ResolveLinks(missionInProgress);
        }


        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            CopyQuantityFromPrimaryLinkOrScaleAsNpc(missionInProgress);
            CheckPrimaryQuantityAndThrow();
            base.ProcessMyQuantity(missionInProgress);
        }


        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidQuantitySet && !ValidPrimaryLinkSet)
            {
                Logger.Error("consistency error! no quantity was set: " + this + " missionId:" + MissionId);
            }

            if (!ValidRangeSet)
            {
                Logger.Error("consistency error! no targetpositionrange was set: " + this + " missionId:" + MissionId);
            }
        }
    }

    [Serializable]
    public class KillRandomTarget : RandomMissionTarget
    {
        public KillRandomTarget(IDataRecord record) : base(record)
        {
            useQuantityOnly = true; //new tech
        }

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_kill_definition(this);
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            if (generateSecondaryDefinition)
            {
                LookUpSecondaryDefinitionOrSelectItem(missionInProgress);
            }

            base.ResolveLinks(missionInProgress);
        }


        public override MissionTargetType Type
        {
            get { return MissionTargetType.kill_definition; }
        }


        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            ProcessPrimaryQuantityAsNpc(missionInProgress);

            ProcessSecondaryQuantity(missionInProgress);

            base.ProcessMyQuantity(missionInProgress);
        }


        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidPrimaryLinkSet && !ValidQuantitySet)
            {
                Logger.Error("consistency error! not linked and no quantity was set: " + this + " missionId:" + MissionId);
            }

            if (generateSecondaryDefinition)
            {
                if (!ValidSecondaryQuantitySet && !ValidSecondaryLinkSet)
                {
                    Logger.Error("consistency error! generate secondary definition is set but not linked and no secondary quantity is set. " + this + " missionId:" + MissionId);
                }
            }
        }
    }

    [Serializable]
    public class LootRandomTarget : RandomMissionTarget
    {

       

        public LootRandomTarget(IDataRecord record) : base(record) {}


        public override MissionTargetType Type
        {
            get { return MissionTargetType.loot_item; }
        }


        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_loot_definition(this);
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            LookupPrimaryDefinitionOrSetFromPools(missionInProgress);
            base.ResolveLinks(missionInProgress);
        }

        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            if (!ValidQuantitySet)
            {
                TryCopyQuantityFromPrimaryLink(missionInProgress);
            }

            CheckPrimaryQuantityAndThrow();

            base.ProcessMyQuantity(missionInProgress);
        }


        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidPrimaryLinkSet && !ValidQuantitySet)
            {
                Logger.Error("consistency error! not linked and no quantity was set: " + this + " missionId:" + MissionId);
            }

          
        }
        
       
    }

    [Serializable]
    public class UseSwitchRandomTarget : MissionStructureTarget
    {
       

        public UseSwitchRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_use_switch(this);
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.use_switch; }
        }


        /// <summary>
        ///     No definition is needed for the switch to operate, skip base class's resolve
        /// </summary>
        /// <param name="missionInProgress"></param>
        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            //force resolution
            IsResolved = true;
        }

        public override void PostLoadedAsConfigTarget()
        {
            if (_levelMultipliers.Length != 10)
                Logger.Error("target must have 10 level multipliers " + this);

            base.PostLoadedAsConfigTarget();
        }

        private readonly double[] _levelMultipliers = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }
    }

    [Serializable]
    public class SubmitItemRandomTarget : MissionStructureTarget
    {
        

        public SubmitItemRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_submit_item(this);
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.submit_item; }
        }


        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            TryScaleByTypeOrCopyPrimaryQuantity(missionInProgress);
            CheckPrimaryQuantityAndThrow();
            base.ProcessMyQuantity(missionInProgress);
        }

        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidQuantitySet && !ValidPrimaryLinkSet)
            {
                Logger.Error("consistency error! not linked and no quantity was set: " + this + " missionId:" + MissionId);
            }

            if (_levelMultipliers.Length != 10)
                Logger.Error("target must have 10 level multipliers " + this);
        }

        private readonly double[] _levelMultipliers = new double[] { 5, 10, 50, 500, 2000, 5000, 10000, 20000, 50000, 100000 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }
    }

    [Serializable]
    public class ItemSupplyRandomTarget : MissionStructureTarget
    {
       

        public ItemSupplyRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_use_itemsupply(this);
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.use_itemsupply; }
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            TryGetResearchableItemFromResearchTarget(missionInProgress);

            base.ResolveLinks(missionInProgress);
        }

        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidQuantitySet && !ValidPrimaryLinkSet)
            {
                Logger.Error("consistency error! no quantity was set: " + this + " missionId:" + MissionId);
            }

            if (_levelMultipliers.Length != 10)
                Logger.Error("target must have 10 level multipliers " + this);
        }

        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            TryScaleByTypeOrCopyPrimaryQuantity(missionInProgress);
            CheckPrimaryQuantityAndThrow();
            base.ProcessMyQuantity(missionInProgress);
        }


        public override void Scale(MissionInProgress missionInProgress)
        {
            CheckMineralAsPrimaryDefinition();

            base.Scale(missionInProgress);
        }



        private readonly double[] _levelMultipliers = new double[] { 5, 10, 50, 500, 2000, 5000, 10000, 20000, 50000, 100000 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }
    }


    [Serializable]
    public class FindArtifactRandomTarget : RandomMissionTarget
    {
        public FindArtifactRandomTarget(IDataRecord record) : base(record)
        {
            useQuantityOnly = true; //new tech
        }

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_find_artifact(this);
        }


        public override MissionTargetType Type
        {
            get { return MissionTargetType.find_artifact; }
        }

        public override bool ResolveLocation(MissionInProgress missionInProgress)
        {
            if (ResolveGenericLocation(missionInProgress))
            {
                return base.ResolveLocation(missionInProgress);
            }

            return false;
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            if (generateSecondaryDefinition)
            {
                //resolve secondary link: this will be the item in the extra loot container spawned on target finished
                LookUpSecondaryDefinitionOrSelectItem(missionInProgress);
            }

            //select an artifact
            var artifactInfo = GetArtifactFromPool(missionInProgress);

            //and set it
            artifactType = (int) artifactInfo.type;

            base.ResolveLinks(missionInProgress);
        }


        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            ScaleArtifactRange(missionInProgress);

            if (FindArtifactSpawnsNpcs)
            {
                ProcessPrimaryQuantityAsNpc(missionInProgress);
            }

            ProcessSecondaryQuantity(missionInProgress);

            base.ProcessMyQuantity(missionInProgress);
        }


        private readonly int[] _artifactRanges = new[] {15, 20, 30, 50, 80, 130, 200, 300, 400, 500};

        private void ScaleArtifactRange(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.ScaleMissionLevel;
            var gangMemberCount = missionInProgress.GangMemberCountMaximized;

            var rawRange = _artifactRanges[level];

            var range = rawRange + rawRange*gangMemberCount* missionDataCache.ScaleArtifactLevelFractionForGangMember;

            TargetPositionRange = (int) (range.Clamp(0, double.MaxValue));

            Log("range scaled. " + TargetPositionRange + " G:" + gangMemberCount + " lvl:" + level);
        }


        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidRangeSet)
            {
                Logger.Error("consistency error! no targetpositionrange was set: " + this + " missionId:" + MissionId);
            }

            if (FindArtifactSpawnsNpcs && !ValidQuantitySet)
            {
                Logger.Error("consistency error! no quantity was set: " + this + " missionId:" + MissionId);
            }

            if (generateSecondaryDefinition)
            {
                if (!ValidSecondaryQuantitySet && !ValidSecondaryLinkSet)
                {
                    Logger.Error("consistency error! generate secondary definition is set but not linked and no secondary quantity is set. " + this + " missionId:" + MissionId);
                }
            }
        }
    }

    [Serializable]
    public class ScanMineralRandomTarget : RandomMissionTarget
    {
        

        public ScanMineralRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_scan_mineral(this);
        }


        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            probeType = (int) MaterialProbeType.Directional;

            if (!ValidDefinitionSet)
            {
                LookUpPrimaryDefinitionOrSelectMineralDefinition(missionInProgress);
            }

            //Cannot use secondary link, this has to generate the geoscan document based on the primary definition
            if (generateSecondaryDefinition)
            {
                //safety
                (!ValidDefinitionSet).ThrowIfTrue(ErrorCodes.ConsistencyError);

                secondaryDefinition = missionDataCache.GetGeoscanDocumentByMineral(Definition);
                secondaryQuantity = 1;
            }

            base.ResolveLinks(missionInProgress);
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.scan_mineral; }
        }

        public override void PostLoadedAsConfigTarget()
        {
            if (_levelMultipliers.Length != 10)
                Logger.Error("target must have 10 level multipliers " + this);

            base.PostLoadedAsConfigTarget();
        }

        private readonly double[] _levelMultipliers = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }
    }

    [Serializable]
    public class LockUnitRandomTarget : RandomMissionTarget
    {
        private long[] _lockedNpcEids;

        public LockUnitRandomTarget(IDataRecord record) : base(record)
        {
            useQuantityOnly = true; //force new tech
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.lock_unit; }
        }

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_lock_unit(this);
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            //primary definition is ignored here
            //deal with secondary, which is the 'gathered intel'

            if (generateSecondaryDefinition)
            {
                //teh intel document
                SetSecondaryDefinitionFromMissionItemsPool(missionInProgress);
            }

            base.ResolveLinks(missionInProgress);
        }

        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            if (ValidQuantitySet)
            {
                ScaleNpcAmount(missionInProgress);
            }
            else
            {
                TryCopyQuantityFromPrimaryLink(missionInProgress);

                if (!ValidQuantitySet)
                {
                    Logger.Error("no quantity was set and not linked. " + this + " missionId:" + MissionId);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }
            }

            if (generateSecondaryDefinition)
            {
                secondaryQuantity = 1;
            }

            CheckPrimaryQuantityAndThrow();

            base.ProcessMyQuantity(missionInProgress);
        }

        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidQuantitySet && !ValidPrimaryLinkSet)
            {
                Logger.Error("consistency error! primary link must be set: " + this + " missionId:" + MissionId);
            }
        }

        public void SetLockedNpcEids(long[] lockedEidList)
        {
            _lockedNpcEids = lockedEidList;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            info["lockedUnits"] = _lockedNpcEids;
            return info;
        }
    }


    [Serializable]
    public class DrillMineralRandomTarget : RandomMissionTarget
    {
        

        public DrillMineralRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_drill_mineral(this);
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            if (!ValidDefinitionSet)
            {
                LookUpPrimaryDefinitionOrSelectMineralDefinition(missionInProgress);
            }

            if (generateSecondaryDefinition)
            {
                //teh proof document
                SetSecondaryDefinitionFromMissionItemsPool(missionInProgress);
            }

            base.ResolveLinks(missionInProgress);
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.drill_mineral; }
        }

        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            TryScaleByTypeOrCopyPrimaryQuantity(missionInProgress);

            if (generateSecondaryDefinition)
            {
                secondaryQuantity = 1;
            }

            CheckPrimaryQuantityAndThrow();

            base.ProcessMyQuantity(missionInProgress);
        }


        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidQuantitySet)
            {
                Logger.Error("consistency error! no quantity was set: " + this + " missionId:" + MissionId);
            }

            if (_levelMultipliers.Length != 10)
                Logger.Error("target must have 10 level multipliers " + this);
        }

        private readonly double[] _levelMultipliers = new double[] { 5, 10, 25, 50, 75, 100, 150, 200, 300, 500 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }

       
    }

    [Serializable]
    public class HarvestPlantRandomTarget : RandomMissionTarget
    {
       

        public HarvestPlantRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_harvest_plant(this);
        }


        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            if (!ValidDefinitionSet)
            {
                LookUpPrimaryDefinitionOrSelectPlantMineralDefinition(missionInProgress);
            }

            if (generateSecondaryDefinition)
            {
                //teh proof document
                SetSecondaryDefinitionFromMissionItemsPool(missionInProgress);
            }

            base.ResolveLinks(missionInProgress);
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.harvest_plant; }
        }

        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            TryScaleByTypeOrCopyPrimaryQuantity(missionInProgress);

            if (generateSecondaryDefinition)
            {
                secondaryQuantity = 1;
            }

            CheckPrimaryQuantityAndThrow();

            base.ProcessMyQuantity(missionInProgress);
        }

        public override void PostLoadedAsConfigTarget()
        {
            if (_levelMultipliers.Length != 10)
                Logger.Error("target must have 10 level multipliers " + this);

            base.PostLoadedAsConfigTarget();
        }

        private readonly double[] _levelMultipliers = new double[] { 5, 10, 25, 50, 75, 100, 150, 200, 300, 500 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }

        
    }

    [Serializable]
    public class FetchItemRandomTarget : RandomMissionTarget
    {
       
        public FetchItemRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_fetch_item(this);
        }


        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            TryCopyDefinitionFromPrimaryLinkedTarget(missionInProgress);

            if (!ValidDefinitionSet)
            {
                Log("the primary link must be set on " + this + " missionId:" + MissionId);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            base.ResolveLinks(missionInProgress);
        }

        public override MissionTargetType Type
        {
            get { return MissionTargetType.fetch_item; }
        }

        public override bool ResolveLocation(MissionInProgress missionInProgress)
        {
            //if the mission structure is not set AND anyLocation is true

            if (!ValidMissionStructureEidSet && !deliverAtAnyLocation)
            {
                //pick a location => fieldTerminal or dockingBase    

                var possibleLocations = missionDataCache.GetAllLocations
                        .Where(l =>
                            l.ZoneConfig.Id == missionInProgress.myLocation.ZoneConfig.Id &&
                            missionInProgress.SearchOrigin.IsInRangeOf2D(l.MyPosition, TargetPositionRange) &&
                            l.LocationEid != missionInProgress.myLocation.LocationEid).ToArray();

                if (possibleLocations.Length == 0)
                {
                    Log("no possible delivery location was found for " + this + " " + missionInProgress);
                    return false;
                }

                Log("possible delivery locations: " + possibleLocations.Length);

                possibleLocations = possibleLocations.Except(missionInProgress.SelectedLocations).ToArray();

                Log("except choosen: " + possibleLocations.Length);

                var minimumDistance = double.MaxValue;
                MissionLocation closestLocation = null;

                foreach (var missionLocation in possibleLocations)
                {
                    var distance = missionLocation.MyPosition.TotalDistance2D(missionInProgress.myLocation.MyPosition);

                    if (distance < minimumDistance)
                    {
                        closestLocation = missionLocation;
                        minimumDistance = distance;
                    }
                }

                //complier shutup
                if (closestLocation == null) return false;

                Log("the closest location is " + closestLocation);

                //this is going to be saved to sql
                missionStructureEid = closestLocation.LocationEid;

                //other data to work with
                targetPositionX = (int) closestLocation.X;
                targetPositionY = (int) closestLocation.Y;
                targetPositionZone = closestLocation.ZoneConfig.Id;

                //comfy init
                SetTargetPosition_RandomTarget();

                //pass the found position on
                missionInProgress.SearchOrigin = targetPosition;

                //and mark the choosen location used
                missionInProgress.AddToSelectedLocations(closestLocation);
            }

            return base.ResolveLocation(missionInProgress);
        }

        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            TryScaleByTypeOrCopyPrimaryQuantity(missionInProgress);
            CheckPrimaryQuantityAndThrow();
            base.ProcessMyQuantity(missionInProgress);
        }


        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidPrimaryLinkSet && !ValidQuantitySet)
            {
                Logger.Error("consistency error! no quantity was set: " + this + " missionId:" + MissionId);
            }

            if (!deliverAtAnyLocation)
            {
                if (!ValidRangeSet)
                {
                    Logger.Error("consistency error! no targetpositionrange was set: " + this + " missionId:" + MissionId);
                }
            }

            if (_levelMultipliers.Length != 10)
                Logger.Error("target must have 10 level multipliers " + this);
        }

        private readonly double[] _levelMultipliers = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }
    }

    [Serializable]
    public class MassproduceRandomTarget : RandomMissionTarget
    {
        public MassproduceRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_massproduce(this);
        }


        public override MissionTargetType Type
        {
            get { return MissionTargetType.massproduce; }
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            DealWithCPRGGenerators(missionInProgress);

            if (!ValidDefinitionSet)
            {
                LookupPrimaryDefinitionOrSetFromPools(missionInProgress);
            }

            base.ResolveLinks(missionInProgress);
        }

        private void DealWithCPRGGenerators(MissionInProgress missionInProgress)
        {
            var source = GetSourceTargetForPrimaryAndSolve(missionInProgress);

            if (source != null)
            {
                //linked to a target which creates a CPRG artificially. 
                //item supply or spawn item currently
                if (!source.myTarget.ValidDefinitionSet)
                {
                    Logger.Error("no cprg definition is set in: " + source);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                if (source.myTarget.PrimaryEntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_random_calibration_programs))
                {
                    Log("linked to a CPRG generator. " + source);

                    //this is the item the CPRG will aid to massproduce
                    var targetDefinition = ProductionDataAccess.GetResultingDefinitionFromCalibrationDefinition(source.myTarget.Definition);

                    definition = targetDefinition;

                    if (!ValidQuantitySet)
                    {
                        quantity = 1; //start scaling :)    
                    }

                    Log("definition is set to CPRG result: " + source.myTarget.Definition + " -> " + targetDefinition);
                }
            }
        }


        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            if (!ValidQuantitySet)
            {
                Logger.Error("no valid quantity is set in " + this + " missionId:" + MissionId);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            if (scalePrimaryQuantityWithLevel)
            {
                ScaleQuantityWithMissionLevel(missionInProgress);
            }

            CheckPrimaryQuantityAndThrow();

            base.ProcessMyQuantity(missionInProgress);
        }


        public override void PostLoadedAsConfigTarget()
        {
            if (!ValidPrimaryLinkSet && !ValidQuantitySet)
            {
                Logger.Error("consistency error! no quantity set " + this + " missionId:" + MissionId);
            }
        }

        private readonly double[] _levelMultipliers = new double[] { 5, 10, 50, 500, 2000, 5000, 10000, 20000, 50000, 100000 };
        public override double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.MissionLevel;
            level = level.Clamp(0, _levelMultipliers.Length - 1);
            return _levelMultipliers[level];
        }
    }

    [Serializable]
    public class ResearchRandomTarget : RandomMissionTarget
    {
        public ResearchRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_research(this);
        }


        public override MissionTargetType Type
        {
            get { return MissionTargetType.research; }
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            LookUpPrimaryDefinitionOrSelectRandomCalibrationProgram(missionInProgress);

            if (ValidDefinitionSet)
            {
                //ALWAYS creates a secondary definition as a new definition injection
                GenerateResearchResultAsSecondaryDefinition(missionInProgress);
            }
            else
            {
                Logger.Error("no valid definition is set in " + this + " missionId:" + MissionId);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            //research, deal with quantity
            quantity = 1;

            base.ResolveLinks(missionInProgress);
        }

        /// <summary>
        ///     Generate the item's definition the ct will create
        /// </summary>
        private void GenerateResearchResultAsSecondaryDefinition(MissionInProgress missionInProgress)
        {
            //the default item related to the CT
            secondaryDefinition = ProductionDataAccess.GetResultingDefinitionFromCalibrationDefinition(Definition);
            Log("Default definition is used for the CPRG");
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            info[k.definition] = secondaryDefinition;
            return info;
        }
    }

    [Serializable]
    public class SpawnItemRandomTarget : RandomMissionTarget
    {
        public SpawnItemRandomTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_MissionTarget_RND_spawn_item(this);
        }


        public override MissionTargetType Type
        {
            get { return MissionTargetType.spawn_item; }
        }

        public override void ResolveLinks(MissionInProgress missionInProgress)
        {
            //handle the case when the spawn item has to spawn the researchable item
            //technically: this linked to a research
            TryGetResearchableItemFromResearchTarget(missionInProgress);

            if (!ValidDefinitionSet)
            {
                LookupPrimaryDefinitionOrSetFromPools(missionInProgress);
            }

            if (!ValidDefinitionSet)
            {
                Logger.Error("no valid definition is set in " + this + " missionId:" + MissionId);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            base.ResolveLinks(missionInProgress);
        }

        protected override void ProcessMyQuantity(MissionInProgress missionInProgress)
        {
            TryScaleByTypeOrCopyPrimaryQuantity(missionInProgress);

            CheckPrimaryQuantityAndThrow();

            base.ProcessMyQuantity(missionInProgress);
        }

        public override void Scale(MissionInProgress missionInProgress)
        {
            CheckMineralAsPrimaryDefinition();
            base.Scale(missionInProgress);
        }


        public override void PostLoadedAsConfigTarget()
        {
            CheckMineralAsPrimaryDefinition();
        }
    }
}
