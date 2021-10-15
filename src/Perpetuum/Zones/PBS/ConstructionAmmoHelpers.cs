using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.Looting;
using System;
using System.Collections.Generic;

namespace Perpetuum.Zones.PBS
{
    public static class ConstructionAmmoHelpers
    {
        private static readonly Lazy<IDictionary<int, EntityDefault>> _constructionBlocksByLevel = new Lazy<IDictionary<int, EntityDefault>>(() =>
        {
            return new Dictionary<int, EntityDefault>()
            {
                {0, EntityDefault.GetByName(DefinitionNames.CONSTRUCTION_MODULE_AMMO_T0) },
                {1, EntityDefault.GetByName(DefinitionNames.CONSTRUCTION_MODULE_AMMO_T1) },
                {2, EntityDefault.GetByName(DefinitionNames.CONSTRUCTION_MODULE_AMMO_T2) },
                {3, EntityDefault.GetByName(DefinitionNames.CONSTRUCTION_MODULE_AMMO_T3) },
            };
        });

        private static EntityDefault GetByLevel(int level)
        {
            return _constructionBlocksByLevel.Value.GetOrDefault(level, EntityDefault.None);
        }

        public static int GetByTargetDefinition(EntityDefault structureDefinition)
        {
            return GetByLevel(structureDefinition.Tier.level).Definition;
        }

        // Loot functions
        private const double DROP_RATE = 0.5;

        public static IEnumerable<LootItem> GetConstructionAmmoLootOnDead(IPBSObject pbsObject)
        {
            var amount = ComputeAmountOnDead(pbsObject);
            var constructionLootList = new List<LootItem>();
            if (amount > 0)
            {
                constructionLootList.Add(BuildConstructionAmmoLoot(pbsObject, amount));
            }
            return constructionLootList;
        }

        public static IEnumerable<LootItem> GetConstructionAmmoLootOnDeconstruct(IPBSObject pbsObject)
        {
            var amount = ComputeAmountOnDeconstruct(pbsObject);
            var constructionLootList = new List<LootItem>();
            if (amount > 0)
            {
                constructionLootList.Add(BuildConstructionAmmoLoot(pbsObject, amount));
            }
            return constructionLootList;
        }

        private static int ComputeAmountOnDeconstruct(IPBSObject pbsObject)
        {
            return (int)(pbsObject.ConstructionLevelMax * DROP_RATE);
        }

        private static int ComputeAmountOnDead(IPBSObject pbsObject)
        {
            var constructionLevelMax = pbsObject.ConstructionLevelMax.Max(1);
            return (int)(constructionLevelMax * DROP_RATE * pbsObject.ConstructionLevelCurrent / constructionLevelMax);
        }

        private static LootItem BuildConstructionAmmoLoot(IPBSObject pbsObject, int amount)
        {
            var ammoDef = GetByTargetDefinition(pbsObject.ED);
            return LootItemBuilder.Create(ammoDef).SetQuantity(amount).Build();
        }
    }
}
