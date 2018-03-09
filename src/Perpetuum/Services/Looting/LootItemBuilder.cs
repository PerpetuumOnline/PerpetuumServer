using System;
using Perpetuum.Builders;
using Perpetuum.Items;

namespace Perpetuum.Services.Looting
{
    public sealed class LootItemBuilder : IBuilder<LootItem>
    {
        private ItemInfo _item;
        private bool _damaged;

        private LootItemBuilder(ItemInfo item)
        {
            _item = item;
        }


        public static LootItemBuilder Create(ItemInfo item)
        {
            return new LootItemBuilder(item);
        }

        public static LootItemBuilder Create(int definition)
        {
            return new LootItemBuilder(new ItemInfo(definition,0));
        }

        public static LootItemBuilder Create(Item item)
        {
            return Create(item.Definition).SetQuantity(item.Quantity).SetRepackaged(item.IsRepackaged).SetHealth(item.Health);
        }

        public LootItemBuilder SetQuantity(int quantity)
        {
            _item.Quantity = quantity;
            return this;
        }

        public LootItemBuilder AsRepackaged()
        {
            return SetRepackaged(true);
        }

        public LootItemBuilder SetRepackaged(bool repackaged)
        {
            _item.IsRepackaged = repackaged;
            return this;
        }

        public LootItemBuilder AsDamaged()
        {
            return SetDamaged(true);
        }

        public LootItemBuilder SetDamaged(bool damaged)
        {
            _damaged = damaged;
            return this;
        }

        public LootItemBuilder SetHealth(double health)
        {
            _item.Health = (float) health;
            return this;
        }

        private const float HEALTH_MODIFIER_LOW = 0.03f;
        private const float HEALTH_MODIFIER_HIGH = 0.97f;

        public LootItem Build()
        {
            if ( !_item.IsRepackaged && _damaged )
                _item.Health *= FastRandom.NextFloat(HEALTH_MODIFIER_LOW,HEALTH_MODIFIER_HIGH);

            var id = Guid.NewGuid();
            return new LootItem(id,_item);
        }
    }
}