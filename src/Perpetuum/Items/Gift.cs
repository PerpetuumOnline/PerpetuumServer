using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Items
{
    public class Gift : Item
    {
        private static readonly List<LootInfo> _loots;

        static Gift()
        {
            _loots = Db.Query().CommandText("select * from giftloots").Execute().Select(r =>
            {
                var definition = r.GetValue<int>("definition");
                var minQuantity = r.GetValue<int>("minquantity");
                var maxQuantity = r.GetValue<int>("maxquantity");
                var q = new IntRange(minQuantity, maxQuantity);
                return new LootInfo(definition,q);
            }).ToList();
        }
        
        public Item Open(Container targetContainer,Character character)
        {
            var randomLoot = _loots.RandomElement();
            var randomItem = (Item)Factory.CreateWithRandomEID(randomLoot.definition);
            randomItem.Owner = character.Eid;
            randomItem.Quantity = FastRandom.NextInt(randomLoot.quantity);
            targetContainer.AddItem(randomItem, false);

            character.LogTransaction(TransactionLogEvent.Builder().SetTransactionType(TransactionType.GiftOpen).SetCharacter(character).SetContainer(targetContainer).SetItem(this));
            character.LogTransaction(TransactionLogEvent.Builder().SetTransactionType(TransactionType.GiftRandomItemCreated).SetCharacter(character).SetContainer(targetContainer).SetItem(randomItem));

            return randomItem;
        }

        private struct LootInfo
        {
            public readonly int definition;
            public readonly IntRange quantity;

            public LootInfo(int definition, IntRange quantity)
            {
                this.definition = definition;
                this.quantity = quantity;
            }
        }
    }
}