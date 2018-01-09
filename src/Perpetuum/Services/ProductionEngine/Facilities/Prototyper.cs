using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public class Prototyper : ProductionFacility
    {
        public static Prototyper CreateWithRandomEID(string definitionName)
        {
            var prototyper = (Prototyper) Factory.CreateWithRandomEID(definitionName);
            prototyper.CreateSystemStorage();
            return prototyper;
        }

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

            return infoDict;
        }

        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.Prototype; }
        }


        public override void OnDeleteFromDb()
        {
            RemoveStorage();
            base.OnDeleteFromDb();
        }

        public override int GetSlotExtensionBonus(Character character)
        {
            return (int)character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_MAX_PROTOTYPER_SLOTS_BASIC);
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
            return character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_PROTOTYPER_MATERIAL_EFFICIENCY_BASIC, ExtensionNames.PRODUCTION_PROTOTYPER_MATERIAL_EFFICIENCY_ADVANCED, ExtensionNames.PRODUCTION_PROTOTYPER_MATERIAL_EFFICIENCY_EXPERT);

        }

       



        public override double GetTimeExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_PROTOTYPER_TIME_EFFICIENCY_BASIC, ExtensionNames.PRODUCTION_PROTOTYPER_TIME_EFFICIENCY_ADVANCED, ExtensionNames.PRODUCTION_PROTOTYPER_TIME_EFFICIENCY_EXPERT);
        }


       
       


       
        public ProductionInProgress StartPrototype(Character character, ProductionDescription productionDescription, Container container, bool useCorporationWallet, out bool hasBonus)
        {
            var foundComponents = productionDescription.SearchForAvailableComponents(container).ToList();

            var materialMultiplier = CalculateMaterialMultiplier(character, productionDescription.definition, out hasBonus);

            var itemsNeeded = productionDescription.ProcessComponentRequirement(ProductionInProgressType.prototype, foundComponents, 1, materialMultiplier);

            //put them to storage
            long[] reservedEids;
            ProductionHelper.ReserveComponents_noSQL(itemsNeeded, StorageEid, container, out reservedEids).ThrowIfError();

            var prototypeTimeSeconds = CalculatePrototypeTimeSeconds(character,  productionDescription.definition);

            prototypeTimeSeconds = GetShortenedProductionTime(prototypeTimeSeconds);

            var productionInProgress = ProductionInProgressFactory();
            productionInProgress.amountOfCycles = 1;
            productionInProgress.baseEID = Parent;
            productionInProgress.character = character;
            productionInProgress.facilityEID = Eid;
            productionInProgress.finishTime = DateTime.Now.AddSeconds(prototypeTimeSeconds);
            productionInProgress.pricePerSecond = GetPricePerSecond();
            productionInProgress.ReservedEids = reservedEids;
            productionInProgress.resultDefinition = productionDescription.GetPrototypeDefinition();
            productionInProgress.startTime = DateTime.Now;
            productionInProgress.totalProductionTimeSeconds = prototypeTimeSeconds;
            productionInProgress.type = ProductionInProgressType.prototype;
            productionInProgress.useCorporationWallet = useCorporationWallet;

            if (!productionInProgress.TryWithdrawCredit())
            {
                if (useCorporationWallet)
                {
                    throw new PerpetuumException(ErrorCodes.CorporationNotEnoughMoney);
                }

                throw new PerpetuumException(ErrorCodes.CharacterNotEnoughMoney);
            }

            return productionInProgress;
        }

        
        public int CalculatePrototypeTimeSeconds(Character character, int targetDefinition)
        {
            //(1+(100/( PC_EXT_PONT+100))*10 *Prototyper.productionTime *targetDefinition.CF.durationMultiplier
            
            var durationModifier = ProductionDataAccess.GetProductionDuration(targetDefinition);
            
            var multiplier = 1 + (100/(GetAdditiveComponentForTime(character) + 100.0));

            var configPrototypeTime = GetProductionTimeSeconds();

            var rawValue = multiplier * 10 * durationModifier * configPrototypeTime; 
            
            if (rawValue > int.MaxValue)
            {
                Logger.Error("overflow in CalculatePrototypeTimeSeconds. definition:" + targetDefinition + " durationModifier:" + durationModifier + " multiplier:" + multiplier + " configPrototypeTime:"+configPrototypeTime + " characterID:" + character.Id);
                rawValue = int.MaxValue;
            }

            return (int) rawValue;
        }


        public long CalculatePrototypePrice(int prototypeTime)
        {
            return (int) (GetPricePerSecond()*prototypeTime);
        }


        private int GetAdditiveComponentForMaterial(Character character)
        {
            var extensionComponent = GetMaterialExtensionBonus(character);
            var standingComponent = GetStandingOfOwnerToCharacter(character) * 20;
            var facilityPoints = GetFacilityPoint();

            return (int) (extensionComponent + facilityPoints + standingComponent);
        }

        private int GetAdditiveComponentForTime(Character character)
        {
            var extensionComponent = GetTimeExtensionBonus(character);
            var standingComponent = GetStandingOfOwnerToCharacter(character) * 20;
            var facilityPoints = GetFacilityPoint();

            return (int)(extensionComponent + facilityPoints + standingComponent);
        }


        private const double HasPrototyperBonus = 5;
        private const double NoPrototyperBonus = 10;

        public double CalculateMaterialMultiplier(Character character, int targetDefinition, out bool hasBonus)
        {
            //(1+(50/(PC_EXT_PONT+PROTOTYPER.ME_PONT +100)))*10*targetDefintion.basematerial
            hasBonus = character.HasTechTreeBonus(ProductionDataAccess.GetOriginalDefinitionFromPrototype(targetDefinition));

            var itemLevel = hasBonus ? HasPrototyperBonus : NoPrototyperBonus;

            var multiplier = (1 + (50/(GetAdditiveComponentForMaterial(character) + 100.0)))*itemLevel;
            return multiplier;
        }

        public override IDictionary<string, object> EndProduction(ProductionInProgress productionInProgress, bool forced)
        {
            return EndPrototype(productionInProgress);
        }

        private IDictionary<string,object> EndPrototype(ProductionInProgress productionInProgress)
        {
            Logger.Info("Prototype finished: " + productionInProgress);

            //delete the used items
            foreach (var item in productionInProgress.GetReservedItems())
            {
                var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.PrototypeDeleted).SetCharacter(productionInProgress.character).SetItem(item);
                productionInProgress.character.LogTransaction(b);

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
                item.Quantity = outputDefault.Quantity*productionInProgress.amountOfCycles;
            });
            container.Save();

            productionInProgress.character.WriteItemTransactionLog(TransactionType.PrototypeCreated, resultItem);

            //get list in order to return

            Logger.Info("EndPrototype created an item: " + resultItem + " production:" + productionInProgress);

            var replyDict = new Dictionary<string, object>
            {
                {k.result, resultItem.BaseInfoToDictionary()},
            };


            ProductionProcessor.EnqueueProductionMissionTarget(MissionTargetType.prototype, productionInProgress.character,MyMissionLocationId(), productionInProgress.resultDefinition);
            return replyDict;
        }

        public override IDictionary<string, object> CancelProduction(ProductionInProgress productionInProgress)
        {
            return ReturnReservedItems(productionInProgress);
        }
    }
}