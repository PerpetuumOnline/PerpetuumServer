using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.ProductionEngine.ResearchKits;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public class ResearchLab : ProductionFacility
    {
        public static ResearchLab CreateWithRandomEID(string definitionName)
        {
            var researchLab = (ResearchLab)Factory.CreateWithRandomEID(definitionName);
            researchLab.CreateSystemStorage();
            return researchLab;
        }

        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var infoData = base.GetFacilityInfo(character);

            var additiveComponent = GetAdditiveComponentForTime(character);
            var additiveComponentCT = GetAdditiveComponent(character);

            infoData.Add(k.myPointsTime, additiveComponent);
            infoData.Add(k.percentageTime, GetPercentageFromAdditiveComponent(additiveComponent));
            infoData.Add(k.timeExtensionPoints, (int)GetTimeExtensionBonus(character));
            infoData.Add(k.extensionCTPoints , (int)GetMaterialExtensionBonus(character));
            infoData.Add(k.myPointsPoints, additiveComponentCT);
            infoData.Add(k.percentagePoints, GetPercentageFromAdditiveComponent(additiveComponentCT));
            return infoData;
        }


        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.Research; }
        }

        private int GetAdditiveComponent(Character character)
        {
            var basePoints = GetFacilityPoint();
            var extensionPoints = GetMaterialExtensionBonus(character);
            var standingPoints = GetStandingOfOwnerToCharacter(character) * 20;

            return (int) (basePoints + extensionPoints + standingPoints);
        }

        private int GetAdditiveComponentForTime(Character character)
        {
            var basePoints = GetFacilityPoint();
            var extensionPoints = GetTimeExtensionBonus(character);
            var standingPoints = GetStandingOfOwnerToCharacter(character) * 20;

            return (int)(basePoints + extensionPoints + standingPoints);
        }

        private int GetAdditiveComponentForTimeWithLevelDifference(Character character, int levelDifference)
        {
            var basePoints = GetFacilityPoint();
            var extensionPoints = GetTimeExtensionBonus(character);
            var standingPoints = GetStandingOfOwnerToCharacter(character) * 20;
            var levelDifferencePoints = levelDifference * 5;

            return (int)(basePoints + extensionPoints + standingPoints + levelDifferencePoints );
        }


        public override void OnDeleteFromDb()
        {
            RemoveStorage();
            base.OnDeleteFromDb();
        }

        public override int GetSlotExtensionBonus(Character character)
        {
            var bonus = character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_MAX_RESEARCH_LAB_SLOTS_BASIC, ExtensionNames.PRODUCTION_MAX_RESEARCH_LAB_SLOTS_ADVANCED, ExtensionNames.PRODUCTION_MAX_RESEARCH_LAB_SLOTS_EXPERT);

            return (int)bonus;
        }


        public override int RealMaxSlotsPerCharacter(Character character)
        {

            //collect extension
            //collect standing
            //calc 
            //[-0.25 ... +0.25]
            double standingRatio = GetStandingOfOwnerToCharacter(character)/40;

            //
            double extensionBonus = GetSlotExtensionBonus(character);
                                                                   


            return 1 + (int) (extensionBonus*(1 + standingRatio));


        }

       

        public override double GetMaterialExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.RESEARCH_CT_EFFICIENCY_BASIC, ExtensionNames.RESEARCH_CT_EFFICIENCY_ADVANCED, ExtensionNames.RESEARCH_CT_EFFICIENCY_EXPERT);
        }



        public void CalculateMaterialAndTimeEfficiency(Character character,ItemResearchLevel itemResearchLevel, int levelDifferencePoints, ref int materialEfficiency, ref int timeEfficiency)
        {
            //baseConfig +decoder.leveldiff*5 + (1-(100/(100+PLAYER_EXTENSION_PONT+Facility_PONT)))*25

            var result = levelDifferencePoints*5 + (1 - (100.0/(100.0 + GetAdditiveComponent(character))))*50;

            materialEfficiency = (int)( materialEfficiency + result);
            timeEfficiency = (int)(timeEfficiency + result);

            Logger.Info("material efficiency:" + materialEfficiency + " time efficiency:" + timeEfficiency);
        }

        public void CalculateFinalResearchTimeSeconds(Character character,  int itemLevel, int researchKitLevel, bool isPrototypeItem, out int researchTime, out int levelDifference)
        {
            levelDifference = researchKitLevel - itemLevel;
            researchTime = GetCharacterNominalResearchTimeSecondsWithLevelDifference(character, levelDifference);
            researchTime *= itemLevel; //item level is multiplying
        }

        public int CalculateNominalResearchTimeSeconds(Character character, int itemLevel)
        {
            var researchTime = GetCharacterNominalResearchTimeSecondsWithLevelDifference(character, 0);
            researchTime *= itemLevel; //item level is multiplying
            return researchTime;
        }
       

        public int GetCharacterNominalResearchTimeSecondsWithLevelDifference(Character character, int levelDifference )
        {
            var baseTime = GetProductionTimeSeconds();

            var nominalTile = (int)((1 + (100.0 / (GetAdditiveComponentForTimeWithLevelDifference(character, levelDifference) + 100))) * baseTime);

            return nominalTile;
        }

        public override double GetTimeExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.RESEARCH_BASIC, ExtensionNames.RESEARCH_ADVANCED, ExtensionNames.RESEARCH_EXPERT);
        }

        public ErrorCodes StartResearch(Character character, int researchTimeSeconds, Item sourceItem,  ResearchKit researchKit, bool useCorporationWallet, out ProductionInProgress newProduction)
        {
            newProduction = ProductionInProgressFactory();

            var itemsList = new [] { researchKit,sourceItem };

            ItemResearchLevel itemResearchLevel;
            if (!ProductionDataAccess.ResearchLevels.TryGetValue(sourceItem.Definition, out itemResearchLevel))
            {
                Logger.Error("consistency error. no research level or calibration program was defined for " + EntityDefault.Get(sourceItem.Definition).Name + " " + sourceItem.Definition);
                return ErrorCodes.ServerError;
            }

            if (itemResearchLevel.calibrationProgramDefinition == null)
            {
                Logger.Error("consistency error. CPRG definition is NULL for " + EntityDefault.Get(sourceItem.Definition).Name + " " + sourceItem.Definition);
                return ErrorCodes.ServerError;
            }

            var cprgDefiniton = (int)itemResearchLevel.calibrationProgramDefinition;


            MoveItemsToStorage(itemsList);

            newProduction = ProductionInProgressFactory();
            newProduction.startTime = DateTime.Now;
            newProduction.finishTime = DateTime.Now.AddSeconds(researchTimeSeconds);
            newProduction.type = ProductionInProgressType.research;
            newProduction.character = character;
            newProduction.facilityEID = Eid;
            newProduction.resultDefinition = cprgDefiniton;
            newProduction.totalProductionTimeSeconds = researchTimeSeconds;
            newProduction.baseEID = Parent;
            newProduction.pricePerSecond = GetPricePerSecond();
            newProduction.ReservedEids = (from i in itemsList select i.Eid).ToArray();
            newProduction.useCorporationWallet = useCorporationWallet;
            newProduction.amountOfCycles = 1;

            if (!newProduction.TryWithdrawCredit())
            {
                if (useCorporationWallet)
                {
                    throw new PerpetuumException(ErrorCodes.CorporationNotEnoughMoney);
                }

                throw new PerpetuumException(ErrorCodes.CharacterNotEnoughMoney);
            }

            return ErrorCodes.NoError;
        }

        private static ErrorCodes LoadItemAndResearchKit(ProductionInProgress productionInProgress, out ResearchKit researchKit, out Item item)
        {
            researchKit = null;
            item = null;

            if (productionInProgress.ReservedEids.Length != 2)
            {
                Logger.Error("illegal amount of reserved items " + productionInProgress);
                return ErrorCodes.ServerError;
            }

            var ed = EntityDefault.GetByEid(productionInProgress.ReservedEids[0]);

            long researchKitEid, itemEid;
            if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_research_kits)  || ed.CategoryFlags.IsCategory(CategoryFlags.cf_random_research_kits))
            {
                researchKitEid = productionInProgress.ReservedEids[0];
                itemEid = productionInProgress.ReservedEids[1];
            }
            else
            {
                itemEid = productionInProgress.ReservedEids[0];
                researchKitEid = productionInProgress.ReservedEids[1];
            }

            item = Item.GetOrThrow(itemEid);
            researchKit = (ResearchKit) Item.GetOrThrow(researchKitEid);

            return ErrorCodes.NoError;
        }

        public override IDictionary<string, object> EndProduction(ProductionInProgress productionInProgress, bool forced)
        {
            return EndResearch(productionInProgress);
        }

        private IDictionary<string,object> EndResearch(ProductionInProgress productionInProgress)
        {
            Logger.Info("research finished: " + productionInProgress);

            Item item;
            ResearchKit researchKit;
            LoadItemAndResearchKit(productionInProgress, out researchKit, out item).ThrowIfError();

            var isPrototypeItem = ProductionDataAccess.IsPrototypeDefinition(item.Definition);

            var itemLevel = ProductionDataAccess.GetResearchLevel(item.Definition);
            var researchKitLevel = researchKit.GetResearchLevel();

            int researchTime;
            int levelDifferenceBonusPoints;
            CalculateFinalResearchTimeSeconds(productionInProgress.character,  itemLevel, researchKitLevel, isPrototypeItem, out researchTime, out levelDifferenceBonusPoints);

            var outputDefinition = productionInProgress.resultDefinition;

            //load public container
            var targetContainer = (PublicContainer) Container.GetOrThrow(PublicContainerEid);
            targetContainer.ReloadItems(productionInProgress.character);

            var outputDefault = EntityDefault.Get(outputDefinition).ThrowIfEqual(EntityDefault.None,ErrorCodes.DefinitionNotSupported);
            (outputDefault.CategoryFlags.IsCategory(CategoryFlags.cf_calibration_programs) || outputDefault.CategoryFlags.IsCategory(CategoryFlags.cf_random_calibration_programs)).ThrowIfFalse(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            

            //create item
            var resultItem = targetContainer.CreateAndAddItem(outputDefinition, false, item1 =>
            {
                item1.Owner = productionInProgress.character.Eid;
                item1.Quantity = 1;
            });

            var calibrationProgram = resultItem as CalibrationProgram;
            calibrationProgram.ThrowIfNull(ErrorCodes.ConsistencyError);
            
            var itemResearchLevel = ProductionDataAccess.GetItemReserchLevelByCalibrationProgram(calibrationProgram);

            int materialEfficiency;
            int timeEfficiency;
            researchKit.GetCalibrationDefaults(outputDefault,  out materialEfficiency, out timeEfficiency);

            var rawMatEff = materialEfficiency;

            //modify the results even further
            CalculateMaterialAndTimeEfficiency(productionInProgress.character, itemResearchLevel, levelDifferenceBonusPoints, ref materialEfficiency, ref timeEfficiency);

            if (calibrationProgram.IsMissionRelated)
            {
                materialEfficiency = rawMatEff;
                calibrationProgram.MaterialEfficiencyPoints = rawMatEff;
                calibrationProgram.TimeEfficiencyPoints = timeEfficiency;    
            }
            else
            {
                calibrationProgram.MaterialEfficiencyPoints = materialEfficiency;
                calibrationProgram.TimeEfficiencyPoints = timeEfficiency; 
            }
            
            

            var randomCalibrationProgram = calibrationProgram as RandomCalibrationProgram;

            //for random missions look up for targets, gang and stuff
            randomCalibrationProgram?.SetComponentsFromRunningTargets(productionInProgress.character);

            calibrationProgram.Save();

            productionInProgress.character.WriteItemTransactionLog(TransactionType.ResearchCreated, calibrationProgram);

            //delete the used items

            Repository.Delete(item);

            Repository.Delete(researchKit);

            productionInProgress.character.WriteItemTransactionLog(TransactionType.ResearchDeleted, item);
            productionInProgress.character.WriteItemTransactionLog(TransactionType.ResearchDeleted, researchKit);

            targetContainer.Save();

            Logger.Info("endResearch created an item: " + calibrationProgram + " production:" + productionInProgress);

            var replyDict = new Dictionary<string, object>
            {
                {k.result, calibrationProgram.ToDictionary()}
            };

           
            ProductionProcessor.EnqueueProductionMissionTarget(MissionTargetType.research, productionInProgress.character, MyMissionLocationId(), calibrationProgram.Definition);
            return replyDict;
        }

        public override IDictionary<string, object> CancelProduction(ProductionInProgress productionInProgress)
        {
            //hatha kell itt meg valamit csinalni
            return ReturnReservedItems(productionInProgress);
        }

        public IDictionary<string, object> ResearchQuery(Character character, int researchKitDefinition, int targetDefinition)
        {
            var replyDict = new Dictionary<string, object>
            {
                {k.researchKitDefinition, researchKitDefinition},
                {k.itemDefinition, targetDefinition}
            };

            var researchKitDefault = EntityDefault.Get(researchKitDefinition);
            var itemDefault = EntityDefault.Get(targetDefinition);

            var missionRelated = false;

            //match item vs research kit vs mission
            if (researchKitDefault.CategoryFlags.IsCategory(CategoryFlags.cf_random_research_kits))
            {
                itemDefault.CategoryFlags.IsCategory(CategoryFlags.cf_generic_random_items).ThrowIfFalse(ErrorCodes.OnlyMissionItemAccepted);
                missionRelated = true;
            }

            if (itemDefault.CategoryFlags.IsCategory(CategoryFlags.cf_generic_random_items) )
            {
                researchKitDefault.CategoryFlags.IsCategory(CategoryFlags.cf_random_research_kits).ThrowIfFalse(ErrorCodes.OnlyMissionResearchKitAccepted);
                missionRelated = true;
            }

            //on gamma not even possible
            if (GetDockingBase().IsOnGammaZone())
            {
                missionRelated.ThrowIfTrue(ErrorCodes.MissionItemCantBeResearchedOnGamma);
            }

            var researchKitLevel = ResearchKit.GetResearchLevelByDefinition(researchKitDefinition);

            replyDict.Add(k.researchKitLevel, researchKitLevel);

            var isPrototypeItem = ProductionDataAccess.IsPrototypeDefinition(targetDefinition);

            Logger.Info("item definition: " + EntityDefault.Get(targetDefinition).Name + " isPrototype:" + isPrototypeItem);

            var nominalDict = new Dictionary<string, object>();
            var realDict = new Dictionary<string, object>();

            //match research levels
            var itemLevel = ProductionDataAccess.GetResearchLevel(targetDefinition);

            itemLevel.ThrowIfEqual(0, ErrorCodes.ItemNotResearchable);
            itemLevel.ThrowIfGreater(researchKitLevel, ErrorCodes.ResearchLevelMismatch);

            var itemResearchLevel = ProductionDataAccess.ResearchLevels.GetOrDefault(targetDefinition).ThrowIfNull(ErrorCodes.ItemNotResearchable);

            var outputDefinition = (int) itemResearchLevel.calibrationProgramDefinition.ThrowIfNull(ErrorCodes.ServerError);

            //calculate 
            CalculateFinalResearchTimeSeconds(character, itemLevel, researchKitLevel, isPrototypeItem, out int researchTimeSeconds, out int levelDifferenceBonusPoints);

            researchTimeSeconds = GetShortenedProductionTime(researchTimeSeconds);

            var price = missionRelated ? 0.0 : researchTimeSeconds * GetPricePerSecond();

            ProductionDataAccess.GetCalibrationDefault(outputDefinition, out int materialEfficiency, out int timeEfficiency);

            var rawMaterialEfficiency = materialEfficiency;

            CalculateMaterialAndTimeEfficiency(character, itemResearchLevel, levelDifferenceBonusPoints, ref materialEfficiency, ref timeEfficiency);

            if (missionRelated)
            {
                //the material efficiency must be default 1.0
                materialEfficiency = rawMaterialEfficiency;
            }


            //calculate nominal
            var nominalResearchTimeSeconds = CalculateNominalResearchTimeSeconds(character, itemResearchLevel.researchLevel);

            var nominalPrice = missionRelated ? 0.0 : nominalResearchTimeSeconds * GetPricePerSecond();

            ProductionDataAccess.GetCalibrationDefault(outputDefinition, out int nominalMaterialEfficiency, out int nominalTimeEfficiency);

            var rawNominalMatEff = nominalMaterialEfficiency;

            CalculateMaterialAndTimeEfficiency(character, itemResearchLevel, 0, ref nominalMaterialEfficiency, ref nominalTimeEfficiency);

            if (missionRelated)
            {
                nominalMaterialEfficiency = rawNominalMatEff;
                researchTimeSeconds = 10;
                nominalResearchTimeSeconds = 10;
            }

            //collect definition related
            replyDict.Add(k.calibrationProgram, itemResearchLevel.calibrationProgramDefinition);

            //collect real
            realDict.Add(k.price, (long) price);
            realDict.Add(k.researchTime, researchTimeSeconds);
            realDict.Add(k.materialEfficiency, materialEfficiency);
            realDict.Add(k.timeEfficiency, timeEfficiency);

            //collect nominal
            nominalDict.Add(k.price, (long) nominalPrice);
            nominalDict.Add(k.researchTime, nominalResearchTimeSeconds);
            nominalDict.Add(k.materialEfficiency, nominalMaterialEfficiency);
            nominalDict.Add(k.timeEfficiency, nominalTimeEfficiency);

            replyDict.Add(k.real, realDict);
            replyDict.Add(k.nominal, nominalDict);
            replyDict.Add(k.facility, Eid);

            return replyDict;
        }
    }
}