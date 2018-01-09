using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Zones;
using Perpetuum.Zones.Artifacts;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    [Serializable]
    public abstract class RandomMissionTarget : MissionTarget
    {
        //use these lookups to add random amount of npcs to the final quantity
        private static readonly List<int> _minRandomNpcPerLevel = new List<int>() {0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 5};
        private static readonly List<int> _maxRandomNpcPerLevel = new List<int>() {1, 1, 1, 2, 2, 3, 3, 4, 5, 5, 5};

        protected RandomMissionTarget(IDataRecord record) : base(record) {}

        public override void AcceptVisitor(MissionTargetVisitor visitor)
        {
            visitor.Visit_RandomMissionTarget(this);
        }

        protected virtual void ProcessMyQuantity(MissionInProgress missionInProgress) { }

        public virtual double GetLevelMultiplier(MissionInProgress missionInProgress)
        {
            return 1.0;
        }
        
        /// <summary>
        /// Advanced operation: build the running target object
        /// </summary>
        /// <param name="missionInProgress"></param>
        /// <returns></returns>
        public override MissionTargetInProgress CreateTargetInProgress(MissionInProgress missionInProgress)
        {
            var missionTarget = GetClone();
            return MissionTargetInProgressFactory(missionInProgress, missionTarget);
        }
        
        public override bool ResolveLocation(MissionInProgress missionInProgress)
        {
            Log("Location found: " + this);

            return base.ResolveLocation(missionInProgress);
        }
        
        public override void PostLoadedAsConfigTarget()
        {
            // most toltottuk be az sqlbol a cachebe itt lehet initelni ha kell

            //NO BASE CALL
        }
        
        /// <summary>
        /// Set - technically cache - the target position from the x,y
        /// </summary>
        protected void SetTargetPosition_RandomTarget()
        {
            targetPosition = new Position(targetPositionX ?? 0, targetPositionY ?? 0);
        }


        protected override bool FilterWork()
        {
            return true; //random target, no filtering
        }

        private void SetPrimaryDefinitionFromItemPools(MissionInProgress missionInProgress)
        {
            var categorFlags = CategoryFlags.cf_generic_random_items;

            if (ValidPrimaryCategorySet)
            {
                categorFlags = PrimaryCategoryFlags;
            }

            definition = GetCombinedDefinitionFromPools(missionInProgress,categorFlags);
        }

        protected void SetSecondaryDefinitionFromMissionItemsPool(MissionInProgress missionInProgress)
        {
            var categoryFlags = CategoryFlags.cf_generic_random_items;

            if (ValidSecondaryCategorySet)
            {
                categoryFlags = SecondaryCategoryFlags;
            }

            secondaryDefinition = GetCombinedDefinitionFromPools(missionInProgress, categoryFlags);

            
        }

        private int GetCombinedDefinitionFromPools(MissionInProgress missionInProgress, CategoryFlags categoryFlags)
        {
            if (Type == MissionTargetType.lock_unit)
            {
                Log("intel document was generated.");
                return EntityDefault.GetByName(DefinitionNames.MISSION_RND_INTEL_DOCUMENT).Definition;
            }

            if (Type == MissionTargetType.drill_mineral)
            {
                Log("mining proof was generated.");
                return EntityDefault.GetByName(DefinitionNames.MISSION_RND_MINING_PROOF).Definition;
            }

            if (Type == MissionTargetType.harvest_plant)
            {
                Log("harvesting proof was generated.");
                return EntityDefault.GetByName(DefinitionNames.MISSION_RND_HARVESTING_PROOF).Definition;
            }


            if (generateResearchKit)
            {
                Log("random research kit was generated.");
                return EntityDefault.GetByName(DefinitionNames.MISSION_RND_RESEARCH_KIT).Definition;
            }

            if (generateCalibrationProgram)
            {
                return GetCalibrationProgramFromPool(missionInProgress);
            }

            return GetDefinitionFromMissionItemsPool(missionInProgress, categoryFlags);
        }

        private int GetCalibrationProgramFromPool(MissionInProgress missionInProgress)
        {
            var possibleRandomCPRGList = EntityDefault.All.GetByCategoryFlags(CategoryFlags.cf_random_calibration_programs).Select(d => d.Definition).ToList();

            Log("possible CPRG definitions:" + possibleRandomCPRGList.Count);

            var exceptCPRGDefinitions = missionInProgress.CollectCPRGDefinitionsFromItems();

            possibleRandomCPRGList = possibleRandomCPRGList.Except(missionInProgress.SelectedItemDefinitions).Except(exceptCPRGDefinitions).ToList();

            Log("except choosen:" + possibleRandomCPRGList.Count);

            if (possibleRandomCPRGList.Count == 0)
            {
                Log("no possible CPRG definitions to select from. " + this + " " + missionInProgress);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }
            
            //now we load the active cprg definitions from the character/gang
            var activeCPRGDefinitions = missionInProgress.CollectActiveCPRGDefinitions();

            Log("active CPRG definitions:" + activeCPRGDefinitions.Count);
            
            possibleRandomCPRGList = possibleRandomCPRGList.Except(activeCPRGDefinitions).ToList();

            Log("except active: " + possibleRandomCPRGList.Count);

            if (possibleRandomCPRGList.Count == 0)
            {
                Log("too many active cprgs running. mission resolve fails " + this + " " + missionInProgress);
                throw new PerpetuumException(ErrorCodes.TooManyActiveCPRG);
            }

            var choosenCPRG = possibleRandomCPRGList.RandomElement();

            //exclude
            missionInProgress.AddToSelectedItems(choosenCPRG);

            //and exclude this as well
            var resultingDefinition = ProductionDataAccess.GetResultingDefinitionFromCalibrationDefinition(choosenCPRG);
            missionInProgress.AddToSelectedItems(resultingDefinition);

            Log("selected CPRG: " + EntityDefault.Get(choosenCPRG).Name + " " + choosenCPRG);

            return choosenCPRG;
        }


        private int GetDefinitionFromMissionItemsPool(MissionInProgress missionInProgress, CategoryFlags categoryFlags)
        {
            
            var possibleDefinitions = EntityDefault.All.GetByCategoryFlags(categoryFlags).Select(d => d.Definition).ToList();

            Log("possible mission item definitions:" + categoryFlags + " " + possibleDefinitions.Count);

            possibleDefinitions = possibleDefinitions.Except(missionInProgress.SelectedItemDefinitions).ToList();

            Log("except choosen: " + possibleDefinitions.Count);

            if (possibleDefinitions.Count == 0)
            {
                Logger.Error("no mission item definition to select from. " + this + " " + missionInProgress);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            var choosenDefinition = possibleDefinitions.RandomElement();

            missionInProgress.AddToSelectedItems(choosenDefinition);

            Log("selected item definition " + choosenDefinition);

            return choosenDefinition;
        }

        protected ArtifactInfo GetArtifactFromPool(MissionInProgress missionInProgress)
        {
            var possibleArtifacts = missionDataCache.GetAllArtifactInfos;

            Log("possible artifacts: " + possibleArtifacts.Count);

            possibleArtifacts = possibleArtifacts.Except(missionInProgress.SelectedArtifactInfos).ToList();

            Log("except choosen: " + possibleArtifacts.Count);

            if (possibleArtifacts.Count == 0)
            {
                Logger.Error("no mission item definition to select from. " + this + " " + missionInProgress);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            var choosenArtifact = possibleArtifacts.RandomElement();

            missionInProgress.AddToSelectedArtifacts(choosenArtifact);

            Log("selected artifact: " + choosenArtifact);

            return choosenArtifact;

        }

        /// <summary>
        /// Designed to handle a singe type npc group. like 3 grphos. Lame.
        /// </summary>
        /// <param name="missionInProgress"></param>
        private void SetDefinitionAsNpcFromPool(MissionInProgress missionInProgress)
        {
            //kivalaszt egy definitiont amit le kell raknia
            var missionLevel = missionInProgress.MissionLevel;
            var raceId = missionInProgress.myLocation.RaceId;

            var npcTemplateRelation = RobotTemplateRelations.GetRandomByMissionLevelAndRaceID(missionLevel, raceId);
            if (npcTemplateRelation == null)
            {
                Logger.Error($"no npc found for missionlevel:{missionLevel} raceid:{raceId}");
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            Log($"selected npc definition {npcTemplateRelation.EntityDefault.Definition}");
            definition = npcTemplateRelation.EntityDefault.Definition;
        }

        private void SetDefinitionAsMineralFromPool(MissionInProgress missionInProgress)
        {
            //find mineral definition to work with

            var possibleDefinitions = missionDataCache.GetPossibleMineralDefinitions(missionInProgress.myLocation.ZoneConfig.Id);

            Log("possible mineral definitions:" + possibleDefinitions.Count);

            possibleDefinitions = possibleDefinitions.Except(missionInProgress.SelectedMinerals).ToList();

            Log("except choosen:" + possibleDefinitions.Count);

            if (possibleDefinitions.Count == 0)
            {
                Log("no possible mineral definition to select from. " + this + " " + missionInProgress);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            var choosenMineral = possibleDefinitions.RandomElement();
            missionInProgress.AddToSelectedMinerals(choosenMineral);

            Log("selected mineral:" + choosenMineral);

            definition = choosenMineral;
        }

        /// <summary>
        /// find mineral definition to work with
        /// </summary>
        /// <param name="missionInProgress"></param>
        private void SetDefinitionAsPlantMineralFromPool(MissionInProgress missionInProgress)
        {
            var possibleDefinitions = GetPossibleHarvestableDefinition(missionInProgress.myLocation.Zone).ToList();

            Log($"possible plant mineral definitions:{possibleDefinitions.Count}");

            possibleDefinitions = possibleDefinitions.Except(missionInProgress.SelectedPlantMinerals).ToList();

            Log($"except choosen:{possibleDefinitions.Count}");

            if (possibleDefinitions.Count == 0)
            {
                Log($"no possible plant mineral definition to select from. {this} {missionInProgress}");
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            var choosenMineral = possibleDefinitions.RandomElement();
            missionInProgress.AddToSelectedPlantMinerals(choosenMineral);

            Log("selected plant mineral:" + choosenMineral);
            definition = choosenMineral;
        }

        private static IEnumerable<int> GetPossibleHarvestableDefinition(IZone zone)
        {
            foreach (var rule in zone.Configuration.PlantRules)
            {
                if (rule.NotFruiting || rule.PlayerSeeded)
                    continue;

                yield return rule.FruitDefinition;
            }
        }


        /// <summary>
        /// Find cprg from pool
        /// </summary>
        /// <param name="missionInProgress"></param>
        private void SetDefinitionAsCPRGFromPool(MissionInProgress missionInProgress)
        {
            definition = GetCalibrationProgramFromPool(missionInProgress);
            quantity = 1;
        }


        protected void LookupPrimaryDefinitionOrSetFromPools(MissionInProgress missionInProgress)
        {
            TryCopyDefinitionFromPrimaryLinkedTarget(missionInProgress);

            if (!ValidDefinitionSet)
            {
                SetPrimaryDefinitionFromItemPools(missionInProgress);
            }
        }

        protected void LookUpPrimaryDefinitionOrSelectNpcDefinition(MissionInProgress missionInProgress)
        {
            TryCopyDefinitionFromPrimaryLinkedTarget(missionInProgress);

            if (!ValidDefinitionSet && !useQuantityOnly)
            {
                //this is a rare case, but it can handle it
                //practically here we suppress the diversity and we only spawn a certain type N times

                //can be aided later with extra npcs above these fixed ones as optional targets
                SetDefinitionAsNpcFromPool(missionInProgress);
            }
        }

        protected void LookUpSecondaryDefinitionOrSelectItem(MissionInProgress missionInProgress)
        {
            if (ValidSecondaryLinkSet)
            {
                if (TryGetResearchableItemFromResearchTarget(missionInProgress, false))
                    return;
                //the item will be used in research
            }

            LookUpSecondaryDefinition(missionInProgress);

            if (!ValidSecondaryDefinitionSet && generateSecondaryDefinition)
            {
                SetSecondaryDefinitionFromMissionItemsPool(missionInProgress);
            }
        }

        protected void CopyZoneInfo(MissionTarget selectedTarget)
        {
            targetPositionX = selectedTarget.targetPosition.intX;
            targetPositionY = selectedTarget.targetPosition.intY;
            targetPositionZone = selectedTarget.ZoneId;
        }

        

        private MissionTarget SearchForPossibleSpots(MissionInProgress missionInProgress)
        {
            var foundTargets = SearchForMinimalAmountOfSpots(missionInProgress, missionInProgress.SelectedTargets);

            if (foundTargets.Count == 0)
            {
                Log("no possible location targets to select from. " + this + " " + missionInProgress);
                return null;
            }

            var selectedTarget = foundTargets.RandomElement();
            missionInProgress.AddToSelectedTargets(selectedTarget);
            return selectedTarget;
        }

       

        protected MissionTarget SelectRandomMissionStructure(MissionInProgress missionInProgress, MissionTargetType targetType)
        {
            var structures = SearchForMinimalAmountOfStructures(missionInProgress, targetType, missionInProgress.SelectedTargets);

            if (structures.Count == 0)
            {
                Log("no possible structure targets to select from. " + targetType + " " + this + " " + missionInProgress);
                return null;
            }

            var selectedTarget = structures.RandomElement();
            missionInProgress.AddToSelectedTargets(selectedTarget);
            return selectedTarget;
        }

        private const int MaxRangeExtend = 600;
        private const int SpotRangeExtend = 100;
        private const int MinimumAmountOfSpots = 5;
        private List<MissionTarget> SearchForMinimalAmountOfSpots(MissionInProgress missionInProgress, List<MissionTarget> alreadySelected)
        {
            var attempt = 1;
            var rangeExtend = 0;
            var spots = GetPossibleMissionSpots(missionInProgress, rangeExtend).Except(alreadySelected).ToList();
            Log(" " + spots.Count + " " + " rnd points found. attempt:" + attempt);

            while (spots.Count < MinimumAmountOfSpots && rangeExtend < MaxRangeExtend)
            {
                attempt++;
                rangeExtend += SpotRangeExtend;
                spots = GetPossibleMissionSpots(missionInProgress, rangeExtend).Except(alreadySelected).ToList();
                
                Log(" " + spots.Count + " " + " rnd points found. range extend:" + rangeExtend + " attempt:" + attempt);

            }

            return spots;

        }

        private const int StructureRangeExtend = 150;
        private const int MinimumAmountOfStructures = 2;
        private List<MissionTarget> SearchForMinimalAmountOfStructures(MissionInProgress missionInProgress, MissionTargetType targetType, List<MissionTarget> alreadySelected)
        {
            var attempt = 1;
            var rangeExtend = 0;
            var structures = GetPossibleStructureTargets(missionInProgress, targetType, rangeExtend).Except(alreadySelected).ToList();

            Log( " " + structures.Count +" " +  targetType +" structures found. attempt:" + attempt);
            

            while (structures.Count < MinimumAmountOfStructures && rangeExtend < MaxRangeExtend)
            {
                attempt++;
                rangeExtend += StructureRangeExtend;
                structures = GetPossibleStructureTargets(missionInProgress, targetType, rangeExtend).Except(alreadySelected).ToList();
                Log(" " + structures.Count + " " + targetType + " structures found. range extend:" + rangeExtend + " attempt:" + attempt);

            }

            return structures;
        }


        private List<MissionTarget> GetPossibleMissionSpots(MissionInProgress missionInProgress, double rangeExtension)
        {
            return missionDataCache.GetAllMissionTargets.Where(t =>
                   (t.Type == MissionTargetType.rnd_point || t.Type == MissionTargetType.find_artifact || t.Type == MissionTargetType.pop_npc) &&
                   t.ValidPositionSet &&
                   t.ValidZoneSet &&
                   missionInProgress.myLocation.ZoneConfig.Id == t.ZoneId &&
                   missionInProgress.SearchOrigin.IsInRangeOf2D(t.targetPosition, t.FindRadius + rangeExtension) &&
                   missionDataCache.IsTargetSelectionValid(missionInProgress.myLocation.Zone,missionInProgress.SearchOrigin,t.targetPosition)
                   ).ToList();
            
        }
        
       
        private List<MissionTarget> GetPossibleStructureTargets(MissionInProgress missionInProgress, MissionTargetType targetType, double rangeExtension)
        {
            return missionDataCache.GetAllMissionTargets
                  .Where(t => t.Type == targetType &&
                              t.ValidFindRadiusSet &&
                              t.ValidMissionStructureEidSet &&
                              t.ValidPositionSet &&
                              t.ValidZoneSet &&
                              t.ZoneId == missionInProgress.myLocation.ZoneConfig.Id &&
                              missionInProgress.SearchOrigin.IsInRangeOf2D(t.targetPosition, t.FindRadius + rangeExtension) &&
                              missionDataCache.IsTargetSelectionValid(missionInProgress.myLocation.Zone,missionInProgress.SearchOrigin,t.targetPosition)
                  ).ToList();
        }



        protected bool ResolveGenericLocation(MissionInProgress missionInProgress)
        {
            var selectedTarget = SearchForPossibleSpots(missionInProgress);

            //search failed
            if (selectedTarget == null) return false;

            CopyZoneInfo(selectedTarget);

            //set the new search origin
            missionInProgress.SearchOrigin = new Position(targetPositionX ?? 0, targetPositionY ?? 0);

            //generate my position value
            SetTargetPosition_RandomTarget();

            return true;
        }


        protected void LookUpPrimaryDefinitionOrSelectMineralDefinition(MissionInProgress missionInProgress)
        {
            TryCopyDefinitionFromPrimaryLinkedTarget(missionInProgress);

            if (!ValidDefinitionSet)
            {
                SetDefinitionAsMineralFromPool(missionInProgress);
            }
        }

        protected void LookUpPrimaryDefinitionOrSelectPlantMineralDefinition(MissionInProgress missionInProgress)
        {
            TryCopyDefinitionFromPrimaryLinkedTarget(missionInProgress);

            if (!ValidDefinitionSet)
            {
                SetDefinitionAsPlantMineralFromPool(missionInProgress);
            }
        }

        protected void LookUpPrimaryDefinitionOrSelectRandomCalibrationProgram(MissionInProgress missionInProgress)
        {
            TryCopyDefinitionFromPrimaryLinkedTarget(missionInProgress);

            if (ValidDefinitionSet)
            {
                //then convert the linked definition to it's CPRG pair
                if (PrimaryEntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_generic_random_items))
                {
                    ItemResearchLevel rl;
                    if (!ProductionDataAccess.ResearchLevels.TryGetValue(Definition, out rl))
                    {
                        Logger.Error("definition is not researchable " + this);
                        throw new PerpetuumException(ErrorCodes.ConsistencyError);
                    }

                    definition = rl.calibrationProgramDefinition;

                    Log("CPRG resolved from link: " + PrimaryEntityDefault.Definition + " " + PrimaryEntityDefault.Name );
                    return;
                }
            }


            if (!ValidDefinitionSet)
            {
                SetDefinitionAsCPRGFromPool(missionInProgress);
            }

           

        }


        private static int GetRandomNpcMinimum(int level)
        {
            level = level.Clamp(0, 9);
            return _minRandomNpcPerLevel[level];
        }

        private static int GetRandomNpcMaximum(int level)
        {
            level = level.Clamp(0, 9);
            return _maxRandomNpcPerLevel[level];
        }

        private static int GetRandomNpcAmountByLevel(int level)
        {
            var min = GetRandomNpcMinimum(level);
            var max = GetRandomNpcMaximum(level);

            return FastRandom.NextInt(min, max);

        }


        protected void ScaleNpcAmount(MissionInProgress missionInProgress)
        {
            var preValue = quantity;
            var level = missionInProgress.ScaleMissionLevel;
            var gangMemberCountMax = missionInProgress.GangMemberCountMaximized;

            var randomAmount = GetRandomNpcAmountByLevel(missionInProgress.MissionLevel);

            quantity = (Quantity + gangMemberCountMax + randomAmount).Clamp(0, int.MaxValue);

            Log("npc amount scaled " + preValue + " -> " + quantity + " Gm:" + gangMemberCountMax + " lvl:" + level + " randAmount:" + randomAmount);
        }

        private void ScaleMineralAmount(MissionInProgress missionInProgress)
        {
            var level = missionInProgress.ScaleMissionLevel;
            var gangMemberCount = missionInProgress.ScaleGangMemberCount;

            var perCycle = missionDataCache.GetAmountPerCycleByMineralDefinition(Definition);

            var rawCycles = GetLevelMultiplier(missionInProgress); //to level raw cycles 

            var smallRandom = FastRandom.NextDouble(0.9, 1.1);

            var scalesCycles = (rawCycles + (gangMemberCount * rawCycles * missionDataCache.ScaleMineralLevelFractionForGangMember)) * PrimaryScaleMultiplier * smallRandom;
            
            var scaledQuantity = scalesCycles * perCycle;
            
            quantity = (int) (Math.Floor(scaledQuantity.Clamp(0, double.MaxValue)));

            Log("scaled as mineral "  + quantity + " G:" + gangMemberCount + " lvl:" + level + " cycles: " + (int)scalesCycles);
        }

        protected void TryCopyQuantityFromPrimaryLink(MissionInProgress missionInProgress)
        {
            //process primary link

            var preValue = quantity;

            var primary = GetSourceTargetForPrimaryAndSolve(missionInProgress);

            if (ValidPrimaryLinkSet && primary != null)
            {
                // primary link is used

                if (!primary.myTarget.IsScaled)
                {
                    Log("primary target not scaled yet. " + primary.TargetType);
                    return;
                }

                if (targetSecondaryAsMyPrimary)
                {
                    //using the linked target's secondary quantity
                    if (primary.myTarget.ValidSecondaryQuantitySet)
                    {
                        quantity = primary.myTarget.SecondaryQuantity;

                        Log("setting priQty from primary link's secQty. " + preValue + " -> " + quantity + "   from:" + primary.myTarget);

                    }
                    else
                    {
                        Logger.Error("the linked target's secondary quantity is not set. current:" + this + "   linked as primary to   " + primary.myTarget);
                        throw new PerpetuumException(ErrorCodes.ConsistencyError);
                    }
                }
                else
                {
                    //default case : using the target's primary quantity
                    if (primary.myTarget.ValidQuantitySet)
                    {
                        quantity = primary.myTarget.Quantity;

                        Log("setting priQty from primary link's priQty. " + preValue + " -> " + quantity + "    from:" + primary.myTarget);
                    }
                    else
                    {
                        Logger.Error("the linked target's primary quantity is not set. current:" + this + "   linked as primary to   " + primary.myTarget);
                        throw new PerpetuumException(ErrorCodes.ConsistencyError);

                    }

                }

            }



        }

        private void TryCopyQuantityFromSecondaryLink(MissionInProgress missionInProgress)
        {
            //process secondary link

            var preValue = quantity;

            var secondary = GetSourceTargetForSecondaryAndSolve(missionInProgress);

            if (ValidSecondaryLinkSet && secondary != null)
            {
                // secondary linked is used

                if (!secondary.myTarget.IsScaled)
                {
                    Log("secondary target not scaled yet. " + secondary.myTarget);
                    return;
                }

                if (targetPrimaryAsMySecondary)
                {
                    //using the linked target's primary quantity
                    if (secondary.myTarget.ValidQuantitySet)
                    {
                        secondaryQuantity = secondary.myTarget.Quantity;
                        Log("setting secQty from secondary link's priQty. " + preValue + " -> " + quantity + "   from:" + secondary.myTarget);
                    }
                    else
                    {
                        Logger.Error("the linked target's primary quantity is not set. current:" + this + "   linked as secondary to   " + secondary.myTarget);
                        throw new PerpetuumException(ErrorCodes.ConsistencyError);
                    }
                }
                else
                {
                    //default case: using the target's secondary quantity
                    if (secondary.myTarget.ValidSecondaryQuantitySet)
                    {
                        secondaryQuantity = secondary.myTarget.SecondaryQuantity;
                        Log("setting secQty from secondary link's secQty. " + preValue + " -> " + quantity + " from:" + secondary.myTarget);
                    }
                    else
                    {
                        Logger.Error("the linked target's secondary quantity is not set. current:" + this + "   linked as secondary to   " + secondary.myTarget);
                        throw new PerpetuumException(ErrorCodes.ConsistencyError);
                    }
                }
            }
        }

        protected void ProcessSecondaryQuantity(MissionInProgress missionInProgress)
        {

            if (generateSecondaryDefinition)
            {
                if (ValidSecondaryQuantitySet)
                {
                    //manual quantity was set

                    if (scaleSecondaryQuantityWithLevel)
                    {
                        //the loot is being scaled
                        ScaleQuantityWithMissionLevel(missionInProgress, false);
                    }
                    else
                    {
                        Log("secondaty quantity left manual: " + SecondaryQuantity);    
                    }
                    
                }
                else
                {
                    TryCopyQuantityFromSecondaryLink(missionInProgress);

                    if (!ValidSecondaryQuantitySet)
                    {
                        //fallback
                        Log("kill loot quantity falls back to 1. " + this + " missionId:" + MissionId);
                        secondaryQuantity = 1;
                    }
                }
            }

            
        }

        protected void ProcessPrimaryQuantityAsNpc(MissionInProgress missionInProgress )
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
                    Logger.Error("quantity is not set and not linked but spawns npcs. " + this + " missionId:" + MissionId);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }
            }
        }


        private bool ProcessQuantityOrSkip(MissionInProgress missionInProgress)
        {
            //links already processed?
            if (!CheckIfLinksAreScaled(missionInProgress)) return false;

            //call the type related functions
            ProcessMyQuantity(missionInProgress);

            return true;
        }


        protected void TryScaleByTypeOrCopyPrimaryQuantity(MissionInProgress missionInProgress)
        {
            if (ValidQuantitySet)
            {
                //quantity manually set, try scaling it by type
                ScalePrimaryQuantityByType(missionInProgress);
            }
            else
            {
                TryCopyQuantityFromPrimaryLink(missionInProgress);
            }
        }

        protected void CheckPrimaryQuantityAndThrow()
        {
            if (!ValidQuantitySet)
            {
                Logger.Error("no valid quantity is set in " + this + " missionId:" + MissionId);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }
        }


        private bool CheckIfLinksAreScaled(MissionInProgress missionInProgress)
        {
            var primary = GetSourceTargetForPrimaryAndSolve(missionInProgress);

            if (primary != null)
            {
                if (!primary.myTarget.IsScaled)
                {
                    Log("primary target not scaled yet. " + primary.myTarget);
                    return false;
                }
            }

            var secondary = GetSourceTargetForSecondaryAndSolve(missionInProgress);

            if (secondary != null)
            {
                if (!secondary.myTarget.IsScaled)
                {
                    Log("secondary target not scaled yet. " + secondary.myTarget);
                    return false;
                }
            }

            return true;
        }


        private void ScalePrimaryQuantityByType(MissionInProgress missionInProgress)
        {
            if (!ValidDefinitionSet)
            {
                Logger.Error("scale fails. no definition is set in " + this);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            var ed = PrimaryEntityDefault;

            if (ValidQuantitySet && ed.CategoryFlags.IsCategory(CategoryFlags.cf_intel_documents))
            {
                //no scaling needed. fetch mining/harvesting proof
                return;
            }

            if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_ore) || ed.CategoryFlags.IsCategory(CategoryFlags.cf_liquid))
            {
                //drillable minerals
                ScaleMineralAmount(missionInProgress);
                return;
            }

            if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_organic))
            {
                //harvestable plants
                ScalePlantFruitAmount(missionInProgress);
                return;
            }

            if (scalePrimaryQuantityWithLevel)
            {
                ScaleQuantityWithMissionLevel(missionInProgress);
                
            }

            //... etc
        }

        protected void ScaleQuantityWithMissionLevel(MissionInProgress missionInProgress, bool processPrimary = true)
        {
            var level = missionInProgress.MissionLevel;
            var gm = missionInProgress.ScaleGangMemberCount +1;

            int whichQuantity;
            if (processPrimary)
            {
                whichQuantity = Quantity;
            }
            else
            {
                whichQuantity = SecondaryQuantity;
            }

            var rawQuantityByLevel = GetLevelMultiplier(missionInProgress);
            var tempQ = whichQuantity * rawQuantityByLevel * gm;

            int preValue;
            if (processPrimary)
            {
                preValue = Quantity;
                quantity = (int)Math.Round(PrimaryScaleMultiplier * tempQ);
                Log("primary scaled " + preValue + " -> " + quantity + " lvl:" + level);
            }
            else
            {
                preValue = SecondaryQuantity;
                secondaryQuantity = (int)Math.Round(SecondaryScaleMultiplier * tempQ);
                Log("secondary scaled " + preValue + " -> " + quantity + " lvl:" + level);
            }
        }


        private void ScalePlantFruitAmount(MissionInProgress missionInProgress)
        {
            //same scaling until...
            ScaleMineralAmount(missionInProgress);
        }

        public override void Scale(MissionInProgress missionInProgress)
        {
            if (ProcessQuantityOrSkip(missionInProgress))
            {
                base.Scale(missionInProgress);    
            }
        }

        /// <summary>
        /// Sets the definition as one of the source items of a research - the item to researh
        /// </summary>
        /// <param name="missionInProgress"></param>
        /// <param name="usePrimaryLink"></param>
        protected bool TryGetResearchableItemFromResearchTarget(MissionInProgress missionInProgress, bool usePrimaryLink = true)
        {
            MissionTargetInProgress linkedTarget;
            if (usePrimaryLink)
            {
                if (!ValidPrimaryLinkSet)
                    return false;

                linkedTarget = GetSourceTargetForPrimaryAndSolve(missionInProgress);
            }
            else
            {
                if (!ValidSecondaryLinkSet)
                    return false;

                linkedTarget = GetSourceTargetForSecondaryAndSolve(missionInProgress);
            }

            if (linkedTarget?.TargetType == MissionTargetType.research)
            {
                if (!linkedTarget.myTarget.ValidDefinitionSet)
                {
                    Logger.Error("no valid cprg definition is set for " + linkedTarget.myTarget);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                //this target is created to aid the research. It must spawn an original item
                //and there must be a way to get the decoder as well along the mission

                //cannot use selected definition exceptions
                var itemDefinition = ProductionDataAccess.GetResultingDefinitionFromCalibrationDefinition(linkedTarget.myTarget.Definition);

                missionInProgress.AddToSelectedItems(itemDefinition);

                //as a side effect it may happen that previous targets choose this definition. dont be surprised!
                
                //possible workaround
                // position the spawn targets as the last/first items, so they will find the item amongst the choosen ones
                // according to research targets in mission

                definition = itemDefinition;
                quantity = 1;

                Log("researchable item resolved:" + PrimaryEntityDefault.Name + " from " + linkedTarget.myTarget );

                return true;
            }

            return false;
        }

        protected void CopyQuantityFromPrimaryLinkOrScaleAsNpc(MissionInProgress missionInProgress)
        {
            if (ValidQuantitySet)
            {
                ScaleNpcAmount(missionInProgress);
                return;
            }
            
            TryCopyQuantityFromPrimaryLink(missionInProgress);


            if (!ValidQuantitySet)
            {
                Logger.Error("target is not linked and no quantity is defined " + this);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }
        }


        protected void TryCopyDefinitionFromPrimaryLinkedTarget(MissionInProgress missionInProgress)
        {
            var primaryLinkedTarget = GetSourceTargetForPrimaryAndSolve(missionInProgress);

            if (ValidPrimaryLinkSet)
            {
                primaryLinkedTarget?.myTarget.CopyMyPrimaryDefinitionToTarget(this);
            }
        }


        private void LookUpSecondaryDefinition(MissionInProgress missionInProgress)
        {
            var secondaryLinkedTarget = GetSourceTargetForSecondaryAndSolve(missionInProgress);

            if (ValidSecondaryLinkSet)
            {
                secondaryLinkedTarget?.myTarget.CopyMySecondaryDefinitionToTarget(this);
            }
        }


        protected MissionTargetInProgress GetSourceTargetForPrimaryAndSolve(MissionInProgress missionInProgress)
        {
            var definitionSourceTarget = GetSourceTargetForPrimaryDefinition(missionInProgress);

            if (definitionSourceTarget != null)
            {
                if (!definitionSourceTarget.myTarget.IsResolved)
                {
                    definitionSourceTarget.myTarget.ResolveLinks(missionInProgress);
                }

            }

            return definitionSourceTarget;
        }


        private MissionTargetInProgress GetSourceTargetForPrimaryDefinition(MissionInProgress missionInProgress)
        {
            if (ValidPrimaryLinkSet)
            {
                MissionTargetInProgress sourceTargetInProgress;
                if (missionInProgress.GetTargetInProgress(PrimaryDefinitionLinkId, out sourceTargetInProgress))
                {
                    return sourceTargetInProgress;
                }
                else
                {
                    Logger.Error("primarydefinitionfromindex is set, but target not found. target:" + this + " mission:" + missionInProgress);
                }
            }

            return null;
        }





        /// <summary>
        /// This function makes sure that the resulting target is solved, 
        /// technically: when we are working with a target we can be sure that it is solved.
        /// </summary>
        /// <param name="missionInProgress"></param>
        /// <returns></returns>
        private MissionTargetInProgress GetSourceTargetForSecondaryAndSolve(MissionInProgress missionInProgress)
        {
            //get secondary target via mission in progress
            var secondaryDefinitionSourceTarget = GetSourceTargetForSecondaryDefinition(missionInProgress);

            if (secondaryDefinitionSourceTarget != null)
            {
                if (!secondaryDefinitionSourceTarget.myTarget.IsResolved)
                {
                    secondaryDefinitionSourceTarget.myTarget.ResolveLinks(missionInProgress);
                }

            }

            return secondaryDefinitionSourceTarget;
        }


        private MissionTargetInProgress GetSourceTargetForSecondaryDefinition(MissionInProgress missionInProgress)
        {
            if (ValidSecondaryLinkSet)
            {
                MissionTargetInProgress sourceTargetInProgress;
                if (missionInProgress.GetTargetInProgress(SecondaryDefinitionLinkId, out sourceTargetInProgress))
                {
                    return sourceTargetInProgress;
                }
                else
                {
                    Logger.Error("secondarydefinitionfromindex is set, but target not found. target:" + this + " mission:" + missionInProgress);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }
            }

            return null;
        }

        protected void CheckMineralAsPrimaryDefinition()
        {
            if (ValidDefinitionSet)
            {
                if (PrimaryEntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_raw_material))
                {
                    Logger.Error("target spawns raw mineral. Not suggested! " + this + " missionId:" + MissionId);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }
            }
        }
    }
}