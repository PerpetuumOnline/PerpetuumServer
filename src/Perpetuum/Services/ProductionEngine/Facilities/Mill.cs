using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public class Mill : ProductionFacility
    {
        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var infoDict = base.GetFacilityInfo(character);

            var additiveComponentMaterial = GetAdditiveComponentForMaterial(character);
            var additiveComponentTime = GetAdditiveComponentForTime(character);

            infoDict.Add(k.myPointsMaterial, additiveComponentMaterial);
            infoDict.Add(k.myPointsTime, additiveComponentTime);

            infoDict.Add(k.percentageMaterial, GetPercentageFromAdditiveComponent(additiveComponentMaterial));
            infoDict.Add(k.percentageTime, GetPercentageFromAdditiveComponent(additiveComponentTime));

            infoDict.Add(k.materialExtensionPoints, (int) GetMaterialExtensionBonus(character));
            infoDict.Add(k.timeExtensionPoints, (int) GetTimeExtensionBonus(character));

            infoDict.Add(k.cycle, 1);
            infoDict.Add(k.rounds, GetMaxRounds(character));
            return infoDict;
        }

        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.MassProduce; }
        }

        public static Mill CreateWithRandomEID(string  definitionName)
        {
            var mill = (Mill)Factory.CreateWithRandomEID(definitionName);
            mill.CreateSystemStorage();
            return mill;
        }

        public override void OnDeleteFromDb()
        {
            RemoveStorage();
            base.OnDeleteFromDb();
        }


        public override int RealMaxSlotsPerCharacter(Character character)
        {
            //collect extension
            //collect standing
            //calc 
            //[-0.25 ... +0.25]
            double standingRatio = GetStandingOfOwnerToCharacter(character) / 40;

            //
            double extensionBonus = GetSlotExtensionBonus(character);

            return 1 + (int) (extensionBonus * (1 + standingRatio));
        }


        public override int GetSlotExtensionBonus(Character character)
        {
            return (int) character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_MAX_MILL_SLOTS_BASIC, ExtensionNames.PRODUCTION_MAX_MILL_SLOTS_ADVANCED, ExtensionNames.PRODUCTION_MAX_MILL_SLOTS_EXPERT);
        }

        public override double GetMaterialExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_MILL_MATERIAL_EFFICIENCY_BASIC, ExtensionNames.PRODUCTION_MILL_MATERIAL_EFFICIENCY_ADVANCED, ExtensionNames.PRODUCTION_MILL_MATERIAL_EFFICIENCY_EXPERT);
        }


        public override double GetTimeExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_MILL_TIME_EFFICIENCY_BASIC, ExtensionNames.PRODUCTION_MILL_TIME_EFFICIENCY_ADVANCED, ExtensionNames.PRODUCTION_MILL_TIME_EFFICIENCY_EXPERT);
        }


        private int GetAdditiveComponentForMaterial(Character character)
        {
            var extensionPoint = GetMaterialExtensionBonus(character);
            var standingPoints = GetStandingOfOwnerToCharacter(character) * 20;
            var facilityPoints = GetFacilityPoint();

            return (int) (facilityPoints + standingPoints + extensionPoint);
        }


        private int GetAdditiveComponentForTime(Character character)
        {
            var extensionComponent = GetTimeExtensionBonus(character);
            var standingComponent = GetStandingOfOwnerToCharacter(character) * 20;
            var facilityPoints = GetFacilityPoint();

            return (int) (facilityPoints + extensionComponent + standingComponent);
        }


        private const int HasMillBonus = 50;
        private const int NoMillBonus = 0;

        private double CalculateFinalMaterialMultiplier(Character character, int lineOrCPRGMaterialPoints, int definition, out bool hasBonus)
        {
            hasBonus = character.HasTechTreeBonus(ProductionDataAccess.GetOriginalDefinitionFromPrototype(definition));

            var millBonus = hasBonus ? HasMillBonus : NoMillBonus;

            var multiplier = 1 + (50.0 / (lineOrCPRGMaterialPoints + GetAdditiveComponentForMaterial(character) + 100.0 + millBonus));
            return multiplier;
        }


        private double CalculateFinalTimeMultiplier(Character character, int targetDefinition, int lineOrCPRGTimePoints)
        {
            var multiplier = 1 + (100 / (lineOrCPRGTimePoints + GetAdditiveComponentForTime(character) + 100.0));

            return multiplier;
        }


        public int CalculateFinalProductionTimeSeconds(Character character, int targetDefinition, int lineOrCPRGTimePoints)
        {

            if (EntityDefault.Get(targetDefinition).CategoryFlags.IsCategory(CategoryFlags.cf_random_items))
            {
                return 10; //fix time for mission items
            }

            //definition related time modifier
            var durationModifier = ProductionDataAccess.GetProductionDuration(targetDefinition);

            var multiplier = CalculateFinalTimeMultiplier(character, targetDefinition, lineOrCPRGTimePoints);

            var facilityProductionTime = GetProductionTimeSeconds();

            return (int) (facilityProductionTime * multiplier * durationModifier);
        }

        public IDictionary<string, object> CalibrateLine(Character character, long calibrationEid, Container container)
        {
            var lineCount = ProductionLine.CountLinesForCharacter(character, Eid);
            var maxSlots = RealMaxSlotsPerCharacter(character);

            lineCount.ThrowIfGreaterOrEqual(maxSlots, ErrorCodes.MaximumAmountOfProducionsReached);

            var calibrationItem = (CalibrationProgram) container.GetItemOrThrow(calibrationEid);
            calibrationItem.Quantity.ThrowIfNotEqual(1, ErrorCodes.ServerError);

            var targetDefinition = calibrationItem.TargetDefinition;
            targetDefinition.ThrowIfEqual(0, ErrorCodes.CPRGNotProducible);
            var targetDefault = EntityDefault.Get(targetDefinition);
            
            calibrationItem.HasComponents.ThrowIfFalse(ErrorCodes.CPRGNotProducible);
            

            //no mission stuff here
            if (calibrationItem.IsMissionRelated || targetDefault.CategoryFlags.IsCategory(CategoryFlags.cf_random_items) )
            {
                if (this.GetDockingBase().IsOnGammaZone())
                {
                    throw new PerpetuumException(ErrorCodes.MissionItemCantBeProducedOnGamma);
                }
            }


            ProductionLine.CreateCalibratedLine(character, Eid, calibrationItem);

            //remove from container
            container.RemoveItemOrThrow(calibrationItem);

            //parent the cprg to the facility
            this.GetStorage().AddChild(calibrationItem);

            calibrationItem.Save();

            container.Save();

            ProductionHelper.ProductionLogInsert(character, targetDefinition, 1, ProductionInProgressType.inserCT, 0, 0, false);

            var informDict = container.ToDictionary();
            var linesList = GetLinesList(character);
            var facilityInfo = GetFacilityInfo(character);

            var replyDict = new Dictionary<string, object>
            {
                {k.lines, linesList},
                {k.lineCount, linesList.Count},
                {k.sourceContainer, informDict},
                {k.facility, facilityInfo}
            };
            return replyDict;
        }


        public IDictionary<string, object> DeleteLine(Character character, int lineId)
        {
            var productionLine = ProductionLine.LoadByIdAndCharacterAndFacility(character, lineId, Eid);

            productionLine.IsDeletable().ThrowIfError();

            ProductionLine.DeleteById(lineId);

            var linesList = GetLinesList(character);
            var facilityInfo = GetFacilityInfo(character);

            var replyDict = new Dictionary<string, object>
            {
                {k.lineCount, linesList.Count},
                {k.lines, linesList},
                {k.facility, facilityInfo}
            };

            return replyDict;
        }


        public ProductionInProgress LineStart(Character character, ProductionLine productionLine, Container sourceContainer, int cycles, bool useCorporationWallet, out bool hasBonus)
        {
            var cprg = productionLine.GetOrCreateCalibrationProgram(this);

            var components = cprg.Components;

            //search for components
            var foundComponents = ProductionHelper.SearchForAvailableComponents(sourceContainer, components).ToList();

            var materialMultiplier = CalculateFinalMaterialMultiplier(character, productionLine.GetMaterialPoints(), productionLine.TargetDefinition, out hasBonus);

            if (cprg.IsMissionRelated)
            {
                //clamp the material multiplier at 1.0
                //so it can ask less that in the mission but never more
                var preMatMult = materialMultiplier;
                materialMultiplier = materialMultiplier.Clamp();
                
                Logger.Info("pre material multiplier:" + preMatMult + " -> " + materialMultiplier);
            }

            //match components
            var itemsNeeded = ProductionHelper.ProcessComponentRequirement(ProductionInProgressType.massProduction, foundComponents, cycles, materialMultiplier, components);

            //put them to storage
            long[] reservedEids;
            ProductionHelper.ReserveComponents_noSQL(itemsNeeded, StorageEid, sourceContainer, out reservedEids).ThrowIfError();

            //calculate time
            var productionTimeSeconds = cycles * CalculateFinalProductionTimeSeconds(character, cprg.TargetDefinition, productionLine.GetTimePoints());

            productionTimeSeconds = GetShortenedProductionTime(productionTimeSeconds);

            if (cprg.IsMissionRelated)
            {
                productionTimeSeconds = 10;
            }

            var newProduction = ProductionInProgressFactory();
            newProduction.startTime = DateTime.Now;
            newProduction.finishTime = DateTime.Now.AddSeconds(productionTimeSeconds);
            newProduction.type = ProductionInProgressType.massProduction;
            newProduction.character = character;
            newProduction.facilityEID = Eid;
            newProduction.resultDefinition = productionLine.TargetDefinition;
            newProduction.totalProductionTimeSeconds = productionTimeSeconds;
            newProduction.baseEID = Parent;
            newProduction.pricePerSecond = cprg.IsMissionRelated ? 0.0 : GetPricePerSecond();
            newProduction.ReservedEids = reservedEids;
            newProduction.amountOfCycles = cycles;
            newProduction.useCorporationWallet = useCorporationWallet;

            if (!newProduction.TryWithdrawCredit())
            {
                //not enough money
                return null;
            }

            //save to sql
            newProduction.InsertProductionInProgess();

            //set running production id to line
            ProductionLine.SetRunningProductionId(productionLine.Id, newProduction.ID).ThrowIfError();

            productionLine.DecreaseRounds();

            sourceContainer.Save();

            Transaction.Current.OnCommited(() =>
            {
                //add to ram
                ProductionProcessor.AddToRunningProductions(newProduction);
            });

            //send info to client
            newProduction.SendProductionEventToCorporationMembersOnCommitted(Commands.ProductionRemoteStart);

            return newProduction;
        }

        public Dictionary<string, object> GetLinesList(Character character)
        {
            var lines = new Dictionary<string, object>();

            var counter = 0;

            foreach (var productionLine in ProductionLine.GetLinesByCharacter(character.Id, Eid))
            {
                var lineDict = productionLine.ToDictionary();

                var result = QueryMaterialAndTime(productionLine.GetOrCreateCalibrationProgram(this), character, productionLine.TargetDefinition, productionLine.GetMaterialPoints(), productionLine.GetTimePoints());

                result.Add(k.line, lineDict);

                lines.Add("l" + counter++, result);
            }

            return lines;
        }

        public Dictionary<string, object> QueryMaterialAndTime(CalibrationProgram calibrationProgram, Character character, int targetDefintion, int lineOrCPRGMaterialPoints, int lineOrCPRGTimePoints, bool forNextRound = false)
        {
            var result = new Dictionary<string, object>();

            if (forNextRound)
            {
                var decalibration = ProductionDataAccess.GetDecalibration(targetDefintion);

                double newMaterialEfficiency = 0;
                double newTimeEfficiency = 0;

                ProductionLine.GetDecalibratedEfficiencies(lineOrCPRGMaterialPoints, lineOrCPRGTimePoints, decalibration.decrease, ref newMaterialEfficiency, ref newTimeEfficiency);

                lineOrCPRGMaterialPoints = (int) newMaterialEfficiency;
                lineOrCPRGTimePoints = (int) newTimeEfficiency;
            }

            bool hasBonus;
            var materialMultiplier = CalculateFinalMaterialMultiplier(character, lineOrCPRGMaterialPoints, targetDefintion, out hasBonus);

            if (calibrationProgram.IsMissionRelated)
            {
                materialMultiplier = materialMultiplier.Clamp(); //never ask more than what we have in the mission
            }
            
            var materials = ProductionDescription.GetRequiredComponentsInfo(ProductionInProgressType.massProduction, 1, materialMultiplier, calibrationProgram.Components);

            materials.Count.ThrowIfEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var productionTimeSeconds = CalculateFinalProductionTimeSeconds(character, targetDefintion, lineOrCPRGTimePoints);

            var price = CalculateProductionPrice(productionTimeSeconds);

            if (calibrationProgram.IsMissionRelated)
            {
                //mission stuff is fixed
                price = 0; 
                productionTimeSeconds = 10;
            }


            result.Add(k.materials, materials);
            result.Add(k.productionTime, productionTimeSeconds);
            result.Add(k.price, price);
            result.Add(k.definition, targetDefintion);
            result.Add(k.materialMultiplier, materialMultiplier);
            result.Add(k.hasBonus, hasBonus);
            result.Add(k.targetQuantity, calibrationProgram.TargetQuantity);
            return result;
        }


        private long CalculateProductionPrice(int productionTime)
        {
            return (long) (productionTime * GetPricePerSecond());
        }

        public override IDictionary<string, object> EndProduction(ProductionInProgress productionInProgress, bool forced)
        {
            return EndMassProduction(productionInProgress, forced);
        }

        private IDictionary<string, object> EndMassProduction(ProductionInProgress productionInProgress, bool forced)
        {
            Logger.Info("mass production finished: " + productionInProgress);

            //delete the used items
            foreach (var item in productionInProgress.GetReservedItems())
            {
                productionInProgress.character.LogTransaction(TransactionLogEvent.Builder()
                                                                   .SetTransactionType(TransactionType.MassProductionDeleted)
                                                                   .SetCharacter(productionInProgress.character)
                                                                   .SetItem(item));
                Repository.Delete(item);
            }

            //pick the output defintion---------------------------------------------------

            var outputDefinition = productionInProgress.resultDefinition;

            //load container
            var container = (PublicContainer) Container.GetOrThrow(PublicContainerEid);

            container.ReloadItems(productionInProgress.character);

            var outputDefault = EntityDefault.Get(outputDefinition).ThrowIfEqual(EntityDefault.None, ErrorCodes.DefinitionNotSupported);

            //create item
            var resultItem = container.CreateAndAddItem(outputDefinition, false, item =>
            {
                item.Owner = productionInProgress.character.Eid;
                item.Quantity = outputDefault.Quantity * productionInProgress.amountOfCycles;
            });

            productionInProgress.character.WriteItemTransactionLog(TransactionType.MassProductionCreated, resultItem);

            CalibrationProgram calibrationProgram;
            var wasLineDead = false;
            var affectedProductionLine = DecalibrateLine(productionInProgress, ref wasLineDead, out calibrationProgram);

            if (affectedProductionLine == null)
            {
                Logger.Error("EndMassProduction: a production line was not found for an ending productionInProgress " + productionInProgress);
            }
            else
            {
                if (!forced && !wasLineDead)
                {
                    if (affectedProductionLine.Rounds >= 1)
                    {
                        //do production rounds
                        //ThreadPoolHelper.ScheduledTask(3000, () => TryNextRound(productionInProgress.character, affectedProductionLine.ID, productionInProgress.amountOfCycles, productionInProgress.useCorporationWallet));

                        var nrp = new NextRoundProduction(ProductionProcessor, productionInProgress.character, affectedProductionLine.Id, productionInProgress.amountOfCycles, productionInProgress.useCorporationWallet, Eid);

                        ProductionProcessor.EnqueueNextRoundProduction(nrp);
                    }
                }
            }


            //mission stuff
            if (outputDefault.CategoryFlags.IsCategory(CategoryFlags.cf_generic_random_items))
            {
                var randomCalibrationProgram = calibrationProgram as RandomCalibrationProgram;
                if (randomCalibrationProgram != null)
                {
                    //set it from the ct
                    resultItem.Quantity = randomCalibrationProgram.TargetQuantity;
                    Logger.Info("mission quantity is forced from CPRG:" + randomCalibrationProgram.Eid  + " qty:" + randomCalibrationProgram.TargetQuantity);
                }
            
            }

            container.Save();

            //get list in order to return
            var linesList = GetLinesList(productionInProgress.character);

            Logger.Info("Mass Production created an item: " + resultItem + " production:" + productionInProgress);

            var replyDict = new Dictionary<string, object>
            {
                {k.result, resultItem.BaseInfoToDictionary()},
                {k.lines, linesList},
                {k.lineCount, linesList.Count}
            };

            ProductionProcessor.EnqueueProductionMissionTarget(MissionTargetType.massproduce, productionInProgress.character, MyMissionLocationId(),  resultItem.Definition, resultItem.Quantity);
            return replyDict;
        }


        public void TryNextRound(Character character, int productionLineId, int cycles, bool useCorporationWallet)
        {
            Logger.Info("trying next round on production line id:" + productionLineId + " characterID:" + character.Id);

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    ProductionLine productionLine;
                    ProductionLine.LoadById(productionLineId, out productionLine).ThrowIfError();

                    productionLine.IsActive().ThrowIfTrue(ErrorCodes.ProductionIsRunningOnThisLine);

                    var sourceContainer = Container.GetOrThrow(PublicContainerEid);

                    sourceContainer.ReloadItems(character);

                    var replyDict = new Dictionary<string, object>();

                    bool hasBonus;
                    var newProduction = LineStart(character, productionLine, sourceContainer, cycles, useCorporationWallet, out hasBonus);

                    if (newProduction == null)
                    {
                        Logger.Info("not enough money to start next round. production line id:" + productionLineId);
                        return;
                    }

                    //return info
                    var linesList = GetLinesList(character);
                    replyDict.Add(k.lines, linesList);
                    replyDict.Add(k.lineCount, linesList.Count);

                    var productionDict = newProduction.ToDictionary();
                    replyDict.Add(k.production, productionDict);

                    var informDict = sourceContainer.ToDictionary();
                    replyDict.Add(k.sourceContainer, informDict);

                    var facilityInfo = GetFacilityInfo(character);
                    replyDict.Add(k.facility, facilityInfo);

                    replyDict.Add(k.hasBonus, hasBonus);

                    Message.Builder.SetCommand(Commands.ProductionLineStart)
                        .WithData(replyDict)
                        .ToCharacter(character)
                        .Send();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    var err = ErrorCodes.SyntaxError;

                    var gex = ex as PerpetuumException;
                    if (gex != null)
                    {
                        err = gex.error;
                    }

                    character.CreateErrorMessage(Commands.ProductionLineStart,err).Send();

                    Logger.Info("production next round start failed: " + err + " characterId:" + character.Id + " lineId:" + productionLineId + " facility:" + this);

                    //silence game logic errors
                    //
                    //extend with more errors if needed
                    if (err != ErrorCodes.RequiredComponentNotFound)
                    {
                        Logger.Exception(ex);
                    }
                }
            }
        }

        public override IDictionary<string, object> CancelProduction(ProductionInProgress productionInProgress)
        {
            var wasLineDead = false;

            CalibrationProgram calibrationProgram;
            var productionLine = DecalibrateLine(productionInProgress, ref wasLineDead, out calibrationProgram);

            if (productionLine == null)
            {
                Logger.Error("Error in CancelProduction, a productionline was not found for the related productionInProgress " + productionInProgress);
            }

            //hatha kell itt meg valamit csinalni)
            var replyDict = ReturnReservedItems(productionInProgress);

            //return list
            var linesList = GetLinesList(productionInProgress.character);
            replyDict.Add(k.lines, linesList);
            replyDict.Add(k.lineCount, linesList.Count);
            return replyDict;
        }


        private ProductionLine DecalibrateLine(ProductionInProgress productionInProgress, ref bool wasLineDead, out CalibrationProgram cprg)
        {
            cprg = null;

            var productionLine = ProductionLine.LoadByProductionId(productionInProgress.character, productionInProgress.ID);
            if (productionLine == null)
            {
                Logger.Error("DecalibrateLine: productionline was not found. a production in progress exists without related productionline. " + productionInProgress);
                return null;
            }

            //this CPRG drives the production
            cprg = productionLine.GetOrCreateCalibrationProgram(this);

           
            // mission mechanism 
            if (cprg.IsMissionRelated)
            {
                //kill the line, delete cprg
                
                wasLineDead = true;

                ProductionLine.DeleteById(productionLine.Id);

                Logger.Info("production line was deleted " + productionLine);

                Repository.Delete(cprg);

                Logger.Info("CPRG deleted from db " + cprg.Eid);
                return productionLine;
            }



            //decalibrate

            var newMaterialEfficiency = productionLine.MaterialEfficiency;
            var newTimeEfficiency = productionLine.TimeEfficiency;

            Logger.Info("pre decalibration mateff:" + newMaterialEfficiency + " timeeff:" + newTimeEfficiency + " " + productionInProgress);

            productionLine.GetDecalibratedEfficiencies(ref newMaterialEfficiency, ref newTimeEfficiency);

            Logger.Info("post decalibration mateff:" + newMaterialEfficiency + " timeeff:" + newTimeEfficiency + " " + productionInProgress);

            productionLine.MaterialEfficiency = newMaterialEfficiency;
            productionLine.TimeEfficiency = newTimeEfficiency;

            if (productionLine.IsAtZero())
            {
                wasLineDead = true;
                Logger.Info("line is dead. " + productionInProgress);

                var info = new Dictionary<string, object>
                {
                    {k.facility, GetFacilityInfo(productionInProgress.character)},
                    {k.line, productionLine.ToDictionary()}
                };

                Message.Builder.SetCommand(Commands.ProductionLineDead)
                    .WithData(info)
                    .ToCharacter(productionInProgress.character)
                    .Send();
            }

            ProductionLine.PostMassProduction(productionInProgress.character, productionLine.Id, newTimeEfficiency, newMaterialEfficiency);

            return productionLine;
        }

       


        public static int GetMaxRounds(Character character)
        {
            var characterLevel = character.GetExtensionLevelSummaryByName(ExtensionNames.LONGTERM_PRODUCTION);

            if (characterLevel == 0)
            {
                return 1;
            }

            return (int) character.GetExtensionBonusByName(ExtensionNames.LONGTERM_PRODUCTION);
        }

        public override void OnRemoveFromGame()
        {
            var linesDeleted = ProductionLine.DeleteAllByFacilityEid(Eid);

            Logger.Info(linesDeleted + " production lines were deleted from " + this);

            var q = 
@"DELETE dbo.runningproductionreserveditem WHERE runningid in
(SELECT id FROM dbo.runningproduction WHERE facilityEID=@facilityEid);
DELETE dbo.runningproduction WHERE facilityEID=@facilityEid";

            Db.Query().CommandText(q)
                .SetParameter("@facilityEid", Eid)
                .ExecuteNonQuery();

            base.OnRemoveFromGame();
        }
    }
}