using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Items;

namespace Perpetuum.Zones.Terrains.Materials
{
    public class RareMaterialHandler
    {
        private readonly ILookup<int, RareMaterialInfo> _rareMaterialInfos;

        public RareMaterialHandler()
        {
            _rareMaterialInfos = Database.CreateLookupCache<int, RareMaterialInfo>("rarematerials", "definition", RareMaterialInfo.CreateFromDbDataRecord);
        }

        public List<ItemInfo> GenerateRareMaterials(int definition)
        {
            var result = new List<ItemInfo>();
            foreach (var rareMaterialInfo in _rareMaterialInfos.GetOrEmpty(definition))
            {
                var random = FastRandom.NextDouble();
                if (random < rareMaterialInfo.chance)
                    result.Add(rareMaterialInfo.itemInfo);
            }
            return result;
        }

        private class RareMaterialInfo
        {
            public readonly ItemInfo itemInfo;
            public readonly double chance;

            private RareMaterialInfo(ItemInfo itemInfo,double chance)
            {
                this.itemInfo = itemInfo;
                this.chance = chance;
            }

            public static RareMaterialInfo CreateFromDbDataRecord(IDataRecord record)
            {
                var definition = record.GetValue<int>("raredefinition");
                var quantity = record.GetValue<int>("quantity");
                var chance = record.GetValue<double>("chance");
                
                return new RareMaterialInfo(new ItemInfo(definition,quantity), chance);
            }
        }
    }
}