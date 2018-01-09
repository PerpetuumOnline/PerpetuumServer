using System;
using System.Collections.Generic;

namespace Perpetuum.Services.Looting
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
}