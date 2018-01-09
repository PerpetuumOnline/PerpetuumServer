using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public class Reprocessor : ProductionFacility
    {
        private readonly ReprocessSession.Factory _reprocessSessionFactory;

        public Reprocessor(ReprocessSession.Factory reprocessSessionFactory)
        {
            _reprocessSessionFactory = reprocessSessionFactory;
        }


        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var infoData = base.GetFacilityInfo(character);

            var additiveComponent = GetAdditiveComponent(character);

            infoData.Add(k.percentageMaterial, GetPercentageFromAdditiveComponent(additiveComponent));
            infoData.Add(k.myPointsMaterial, additiveComponent);
            infoData.Add(k.extensionPoints, (int)GetMaterialExtensionBonus(character));

            return infoData;
        }


        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.Reprocess; }
        }

        private int GetAdditiveComponent(Character character)
        {
            var extensionBonus = GetMaterialExtensionBonus(character);
            var standingComponent = GetStandingOfOwnerToCharacter(character) * 20;
            var facilityComponent = GetFacilityPoint();

            return (int) (standingComponent + extensionBonus + facilityComponent);
        }


        public double GetMaterialMultiplier(Character character)
        {
            var multiplier = 1 - (75/(GetAdditiveComponent(character) + 100.0));
            return multiplier;
        }

        public override int RealMaxSlotsPerCharacter(Character character)
        {
            return 1;
        }

        public override double GetMaterialExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_REPROCESS);
        }

        public ReprocessSession CollectReprocessSession(Character character, Container container, IEnumerable<long> targetEids)
        {
            var ec = ErrorCodes.NoError;
            var materialMultiplier = GetMaterialMultiplier(character);

#if (DEBUG)
            Logger.Info("material multiplier for reprocess: " + materialMultiplier);
#endif
            var reprocessSession = _reprocessSessionFactory();
            foreach (var targetEid in targetEids)
            {
                var targetItem = container.GetItem(targetEid, true);
                if (targetItem == null)
                    continue;

                if ((ec = ProductionHelper.CheckReprocessCondition(targetItem, character)) != ErrorCodes.NoError)
                    continue;

                reprocessSession.AddMember(targetItem, materialMultiplier, character);
            }

            if (targetEids.Count() == 1)
            {
                ec.ThrowIfNotEqual(ErrorCodes.NoError, ec);
            }

            return reprocessSession;
        }

        public IDictionary<string, object> ReprocessQuery(Character character, Container container, IEnumerable<long> targetEids)
        {
            var reprocessSession = CollectReprocessSession(character, container, targetEids);

            //return verbose result

            var replyDict = new Dictionary<string, object>();
            var itemsDict = reprocessSession.GetQueryDictionary();
            replyDict.Add(k.items, itemsDict);
            replyDict.Add(k.facility, Eid);

            return replyDict;
        }
    }
}