using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.ExportedTypes;

using Perpetuum.Groups.Corporations;
using Perpetuum.Items;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public class Repair : ProductionFacility
    {
        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var infoData = base.GetFacilityInfo(character);

            var additiveComponent = GetAdditiveComponent(character);
            
            infoData.Add(k.myPointsCredit, additiveComponent);
            infoData.Add(k.percentageCredit, GetPercentageFromAdditiveComponent( additiveComponent));
            infoData.Add(k.extensionPoints,(int)GetMaterialExtensionBonus(character));

            return infoData;
        }

        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.Repair; }
        }

        private int GetAdditiveComponent(Character character)
        {
            var standing = GetStandingOfOwnerToCharacter(character);
            if (standing < 0.0)
                standing = 0.0;

            var standingComponent = standing * 20;
            var extensionComponent = GetMaterialExtensionBonus(character);
            var facilityPoints = GetFacilityPoint();

            return (int) (standingComponent + extensionComponent + facilityPoints);
        }

        private int GetRepairPrice(Character character,Item item)
        {
            var finalRatio = GetRepairRatio(character);
            return (int) ((1 - Math.Min(1,item.HealthRatio))* PriceCalculator.GetAveragePrice(item) * finalRatio);
        }

        private double GetRepairRatio(Character character)
        {
            //(75/(PC_STANDIING + PC_EXT_PONT+REPAIRER.ME_PONT+100))
            var multiplier = (75/(GetAdditiveComponent(character) + 100.0));
            return multiplier;
        }

        public override int RealMaxSlotsPerCharacter(Character character)
        {
            return 1;
        }

        public override double GetMaterialExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_REPAIR_EXPERT, ExtensionNames.PRODUCTION_REPAIR_ADVANCED, ExtensionNames.PRODUCTION_REPAIR_BASIC);
        }

        public void RepairItems(Character character, IEnumerable<long> targetEiDs, Container sourceContainer, bool useCorporationWallet)
        {
            var wallet = character.GetWallet(useCorporationWallet, TransactionType.ProductionMultiItemRepair);

            var b = TransactionLogEvent.Builder()
                                       .SetCharacter(character)
                                       .SetTransactionType(TransactionType.ProductionMultiItemRepair);

            var corpWallet = wallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
            }

            var sumPrice = 0.0;
            foreach (var item in sourceContainer.SelectDamagedItems(targetEiDs).ToArray())
            {
                try
                {
                    var price = GetRepairPrice(character, item);

                    wallet.Balance -= price;

                    b.SetCreditBalance(wallet.Balance).SetCreditChange(-price).SetItem(item);

                    if (corpWallet != null)
                    {
                        corpWallet.Corporation.LogTransaction(b);
                    }
                    else
                    {
                        character.LogTransaction(b);
                    }

                    item.Repair();

                    //ha nem robot akkor csomagoljuk be
                    if (item.ED.AttributeFlags.Repackable && !item.ED.CategoryFlags.IsCategory(CategoryFlags.cf_robots))
                    {
                        item.IsRepackaged = true;
                    }

                    sumPrice += price;
                }
                catch (PerpetuumException gex)
                {
                    character.SendItemErrorMessage(Commands.ProductionRepair, gex.error,item);
                }
            }

            GetDockingBase().AddCentralBank(TransactionType.ProductionMultiItemRepair, sumPrice);
            sourceContainer.Save();
        }

        public IDictionary<string,object> QueryPrices(Character character, Container container, IEnumerable<long> targetEids)
        {
            var prices = container.SelectDamagedItems(targetEids).ToDictionary("e", item => new Dictionary<string, object>
            {
                {k.eid, item.Eid},
                {k.price, GetRepairPrice(character,item)},
                {k.health, item.HealthRatio}
            });

            return new Dictionary<string, object> {{"prices", prices}};
        }
    }
}