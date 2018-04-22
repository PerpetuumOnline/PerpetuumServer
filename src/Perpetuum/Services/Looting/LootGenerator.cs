using System.Collections.Generic;

namespace Perpetuum.Services.Looting
{

    public class LootGenerator : ILootGenerator
    {
        private readonly IEnumerable<LootGeneratorItemInfo> _lootInfos;

        public LootGenerator(IEnumerable<LootGeneratorItemInfo> lootInfos)
        {
            _lootInfos = lootInfos;
        }

        public IEnumerable<LootItem> Generate()
        {
            foreach (var info in _lootInfos)
            {
                if (FastRandom.NextDouble() >= info.probability)
                    continue;

                var lootItem = LootItemBuilder.Create(info.item).SetDamaged(info.damaged).Build();
                lootItem.Quantity = lootItem.ItemInfo.randomQuantity(); //roll random on generate
                yield return lootItem;
            }
        }
    }
}