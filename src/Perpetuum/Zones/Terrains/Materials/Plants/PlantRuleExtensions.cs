using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Terrains.Materials.Plants
{
    public static class PlantRuleExtensions
    {
        [CanBeNull]
        public static PlantRule GetPlantRule(this IEnumerable<PlantRule> rules, PlantType plantType)
        {
            return plantType == PlantType.NotDefined ? null : rules.FirstOrDefault(r => r.Type == plantType);
        }

        public static IDictionary<string, object> GetPlantInfoForClient(this IEnumerable<PlantRule> rules)
        {
            return rules.ToDictionary("p", r => r.ToDictionary());
        }

       

        public static PlantType GetWinnerPlantTypeBasedOnFertility(this PlantRule[] rules)
        {
            var list = new List<PlantType>(rules.Length*rules.Length);

            foreach (var plantRule in rules.Where(r => !r.PlayerSeeded))
            {
                for (var i = 0; i < plantRule.Fertility; i++)
                {
                    list.Add(plantRule.Type);
                }
            }

            return list.RandomElement();
        }

        // index -> count
        public static PlantType GetSpreadingBasedWinnerPlantType(this PlantRule[] rules, Dictionary<PlantType, int> neighbouringPlants)
        {
            var list = new List<PlantType>(neighbouringPlants.Count*neighbouringPlants.Count);

            foreach (var kvp in neighbouringPlants)
            {
                var rule = rules.FirstOrDefault(r => r.Type == kvp.Key);
                if (rule == null || rule.PlayerSeeded || rule.Spreading <= 0)
                    continue;

                for (var i = 0; i < kvp.Value * rule.Spreading; i++)
                {
                    list.Add(rule.Type);
                }
            }

            return list.RandomElement();

        }


    }
}