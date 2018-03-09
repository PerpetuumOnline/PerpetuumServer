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
                // set quantity for this item.
                lootItem.Quantity = (info.item.MinQty != info.item.MaxQty) ? FastRandom.NextInt(info.item.MinQty, info.item.MaxQty) : info.item.MinQty;
                yield return lootItem;
            }
        }
    }
}