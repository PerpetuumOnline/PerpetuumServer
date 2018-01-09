using System;
using System.Collections.Generic;
using System.Linq;
using GenXY.Framework;

namespace Perpetuum.Zones.LootContainers
{
    public interface ILootItemRepository
    {
        void Add(LootContainer container, LootItem lootItem);
        void Update(LootContainer container, LootItem lootItem);
        void Delete(LootContainer container, LootItem item);
        void DeleteAll(LootContainer container);
        bool IsEmpty(LootContainer container);
        [CanBeNull]
        LootItem Get(LootContainer container,Guid id);
        IEnumerable<LootItem> GetAll(LootContainer container);
        IEnumerable<LootItem> GetByDefinition(LootContainer container, int definition);
    }

    public static class LootItemRepositoryExtensions
    {
        public static void AddMany(this ILootItemRepository repository, LootContainer container, IEnumerable<LootItem> lootItems)
        {
            foreach (var lootItem in lootItems)
            {
                repository.AddWithStack(container,lootItem);
            }
        }

        public static void AddWithStack(this ILootItemRepository repository, LootContainer container, LootItem lootItem)
        {
            var f = repository.GetByDefinition(container,lootItem.ItemInfo.Definition).FirstOrDefault(l => Math.Abs(l.ItemInfo.Health - lootItem.ItemInfo.Health) < double.Epsilon);
            if (f == null)
            {
                repository.Add(container,lootItem);
                return;
            }

            f.Quantity += lootItem.Quantity;
            repository.Update(container,f);
        }
    }
}