using Perpetuum;
using Perpetuum.Builders;
using Perpetuum.Data;
using Perpetuum.Services.Looting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Services.Relics
{

    public class RelicLootGenerator
    {
        private readonly RelicLootReader _relicLootRepository;
        private Random _random;

        public RelicLootGenerator()
        {
            _relicLootRepository = new RelicLootReader();
            _random = new Random();
        }

        //Guard loot-loop from empty or low probability loot tables (would indicate bad/missing Relicloot entries)
        private bool HasValidLoots(IRelicLoot[] loots)
        {
            return loots.Length > 0 && (0.1 < loots.Sum(loot => loot.Chance));
        }

        public RelicLootItems GenerateLoot(RelicInfo relicInfo)
        {
            var result = new List<LootItem>();

            var loots = _relicLootRepository.GetRelicLoots(relicInfo).ToArray();

            if (!HasValidLoots(loots))
                return null;

            do
            {
                foreach (var loot in loots)
                {
                    var chance = _random.NextDouble();
                    if (chance > loot.Chance)
                        continue;

                    var builder = loot.GetLootItemBuilder();
                    var lootItem = builder.Build();
                    result.Add(lootItem);
                }

            } while (!result.Any());

            return new RelicLootItems(result);
        }
    }


    public class RelicLootItems
    {
        public IEnumerable<LootItem> LootItems { get; private set; }

        public RelicLootItems(IEnumerable<LootItem> lootItems)
        {
            LootItems = lootItems;
        }
    }


    public interface IRelicLoot
    {
        double Chance { get; }
        IBuilder<LootItem> GetLootItemBuilder();
    }

    /// <summary>
    /// Describes one loot item can be found in a discovered relic
    /// </summary>
    public class RelicLoot : IRelicLoot
    {
        private int Definition { get; set; }
        private IntRange Quantity { get; set; }
        public double Chance { get; private set; }

        public IBuilder<LootItem> GetLootItemBuilder()
        {
            return LootItemBuilder.Create(Definition)
                .SetQuantity(FastRandom.NextInt(Quantity))
                .SetRepackaged(Packed);
        }

        private bool Packed { get; set; }
        private int RelicInfoId { get; set; }

        public RelicLoot(IDataRecord record)
        {
            Definition = record.GetValue<int>("definition");
            Quantity = new IntRange(record.GetValue<int>("minquantity"), record.GetValue<int>("maxquantity"));
            Chance = (float)record.GetValue<decimal>("chance");
            Packed = record.GetValue<bool>("packed");
            RelicInfoId = record.GetValue<int>("relictypeid");

        }

    }


    public class RelicLootReader
    {
        protected IRelicLoot CreateRelicLootFromRecord(IDataRecord record)
        {
            return new RelicLoot(record);
        }

        public IEnumerable<IRelicLoot> GetRelicLoots(RelicInfo info)
        {
            var loots = Db.Query().CommandText("SELECT definition,minquantity,maxquantity,chance,relictypeid,packed FROM relicloot WHERE relictypeid = @relicInfoId")
                .SetParameter("@relicInfoId", info.GetID())
                .Execute()
                .Select(CreateRelicLootFromRecord);

            return loots.ToList();
        }
    }
}
