using Perpetuum.Items;
using Perpetuum.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.Looting
{

    public class LootGenerator : ILootGenerator
    {
        private readonly IEnumerable<LootGeneratorItemInfo> _lootInfos = new List<LootGeneratorItemInfo>();

        public LootGenerator(IEnumerable<LootGeneratorItemInfo> lootInfos)
        {
            _lootInfos = lootInfos;
        }

        public IReadOnlyCollection<LootGeneratorItemInfo> GetInfos()
        {
            return this._lootInfos.ToList();
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


    public class SplittableLootGenerator : ISplittableLootGenerator
    {
        private readonly IReadOnlyCollection<LootGeneratorItemInfo> _lootInfos = new List<LootGeneratorItemInfo>();
        private readonly Random _random;

        public SplittableLootGenerator(ILootGenerator lootGenerator)
        {
            _lootInfos = lootGenerator.GetInfos();
            _random = new Random();
        }

        public List<ILootGenerator> GetGenerators(int splitCount)
        {
            List<LootGeneratorItemInfo> generatorInfos = new List<LootGeneratorItemInfo>();

            foreach (var info in _lootInfos)
            {
                var lootItem = LootItemBuilder.Create(info.item).SetDamaged(info.damaged).Build();
                lootItem.Quantity = lootItem.ItemInfo.randomQuantity();
                var quantity = lootItem.Quantity;
                var splitQuantity = quantity / splitCount;
                var remainder = quantity % splitCount;
                var amounts = new int[splitCount];
                for (var i = 0; i < splitCount; i++)
                {
                    var amount = splitQuantity + (remainder > 0 ? 1 : 0);
                    remainder--;
                    amounts[i] = amount;
                }
                amounts = amounts.OrderBy(x => _random.Next()).ToArray();
                for (var i = 0; i < splitCount; i++)
                {
                    ItemInfo itemInfo = new ItemInfo(lootItem.ItemInfo.Definition, amounts[i], amounts[i]);
                    var generatorInfo = new LootGeneratorItemInfo(itemInfo, info.damaged, info.probability);
                    generatorInfos.Add(generatorInfo);
                }
            }

            int counter = 0;
            List<LootGeneratorItemInfo>[] lootGeneratorItemInfos = new List<LootGeneratorItemInfo>[splitCount];
            foreach (var generatorInfo in generatorInfos)
            {
                if (lootGeneratorItemInfos[counter] == null)
                {
                    lootGeneratorItemInfos[counter] = new List<LootGeneratorItemInfo>();
                }
                lootGeneratorItemInfos[counter].Add(generatorInfo);

                counter++;
                counter %= splitCount;
            }

            List<ILootGenerator> lootGenerators = new List<ILootGenerator>();
            for (var i = 0; i < splitCount; i++)
            {
                lootGenerators.Add(new LootGenerator(lootGeneratorItemInfos[i]));
            }

            return lootGenerators;
        }

    }
}