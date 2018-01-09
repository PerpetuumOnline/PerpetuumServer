using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Robots;

namespace Perpetuum.Items.Templates
{
    public class RobotInventoryTemplate : ItemTemplate<RobotInventory>
    {
        private readonly ItemTemplate<Item>[] _itemTemplates;

        private RobotInventoryTemplate(ItemTemplate<Item>[] items) : base(1,false)
        {
            _itemTemplates = items ?? new ItemTemplate<Item>[0];
        }

        public ItemTemplate<Item>[] Items
        {
            get { return _itemTemplates; }
        }

        protected override void OnBuild(RobotInventory robotInventory)
        {
            foreach (var itemTemplate in _itemTemplates)
            {
                var i = itemTemplate.Build();
                robotInventory.AddChild(i);
            }

            base.OnBuild(robotInventory);
        }

        protected override bool OnValidate(RobotInventory robotInventory)
        {
            if (!_itemTemplates.All(i => i.Validate()))
                return false;

            return base.OnValidate(robotInventory);
        }

        public static RobotInventoryTemplate Create(int definition, ItemTemplate<Item>[] itemTemplates)
        {
            return new RobotInventoryTemplate(itemTemplates) { EntityDefault = EntityDefault.Get(definition) };
        }
    }
}