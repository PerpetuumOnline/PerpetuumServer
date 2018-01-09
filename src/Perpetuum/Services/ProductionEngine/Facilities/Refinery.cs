using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public class Refinery : ProductionFacility
    {
        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var infoData = base.GetFacilityInfo(character);

            var additiveComponent = GetAdditiveComponent(character);

            infoData.Add(k.percentageMaterial, GetPercentageFromAdditiveComponent(additiveComponent));
            infoData.Add(k.myPointsMaterial,additiveComponent );
            infoData.Add(k.extensionPoints, (int)GetMaterialExtensionBonus(character));

            return infoData;
        }

        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.Refine; }
        }

        public IDictionary<string,object> RefineQuery(Character character, int targetDefinition, int targetAmount, ProductionDescription productionDescription)
        {
            var replyDict = new Dictionary<string, object>();

            var materialEfficiency = GetMaterialMultiplier(character);
            var components = productionDescription.Components
                .Where(c => !c.IsSkipped(ProductionInProgressType.refine))
                .ToDictionary("c", component => new Dictionary<string, object>
                {
                    {k.definition, component.EntityDefault.Definition},
                    {k.real, component.EffectiveAmount(targetAmount, materialEfficiency)},
                    {k.nominal, component.Amount*targetAmount}
                });

            //these are the nominal and the real amounts
            replyDict.Add(k.components, components);

            //requested amount
            replyDict.Add(k.targetAmount, targetAmount);

            //requested definition
            replyDict.Add(k.targetDefinition, targetDefinition);

            //requested facility
            replyDict.Add(k.facility, Eid);

            return replyDict;
        }

        public IDictionary<string,object> Refine(Character character, Container sourceContainer, int targetAmount, ProductionDescription productionDescription)
        {
            var materialMultiplier = GetMaterialMultiplier(character);

            //collect availabe materials
            var foundComponents = productionDescription.SearchForAvailableComponents(sourceContainer).ToList();

            //generate a list of the used components
            var itemsUsed = productionDescription.ProcessComponentRequirement(ProductionInProgressType.refine, foundComponents, targetAmount, materialMultiplier);

            //create item
            productionDescription.CreateRefineResult(character.Eid, sourceContainer, targetAmount, character);

            //update / delete components from source container
            ProductionDescription.UpdateUsedComponents(itemsUsed, sourceContainer, character, TransactionType.RefineDelete).ThrowIfError();

            sourceContainer.Save();

            var sourceInformData = sourceContainer.ToDictionary();

            var replyDict = new Dictionary<string, object> {{k.sourceContainer, sourceInformData}};
            return replyDict;
        }

        private int GetAdditiveComponent(Character character)
        {
            var extensionPoints = GetMaterialExtensionBonus(character);
            var refinerypoints = GetFacilityPoint();
            var standingPoints = GetStandingOfOwnerToCharacter(character) * 20;

            return (int)( extensionPoints + refinerypoints + standingPoints);
        }


        private double GetMaterialMultiplier(Character character)
        {
            //(1+(50/(STANDING + PC_EXT_PONT + REFINERY.ME_PONT + 100)))

            var multiplier = (1 + (50/(GetAdditiveComponent(character) + 100.0)));
            
            return multiplier;

        }

        public override int RealMaxSlotsPerCharacter(Character character)
        {
            return 1;
        }

        public override double GetMaterialExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_REFINE);
        }
    }
}