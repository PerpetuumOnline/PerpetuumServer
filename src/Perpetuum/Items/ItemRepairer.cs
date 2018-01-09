using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Robots;

namespace Perpetuum.Items
{
    public class ItemRepairer : IEntityVisitor<Item>,IEntityVisitor<Robot>,IEntityVisitor<RobotInventory>
    {
        private static readonly  ItemRepairer _default = new ItemRepairer();

        public void Visit(Item item)
        {
            item.SetMaxHealth();

            foreach (var child in item.Children.OfType<Item>())
            {
                child.AcceptVisitor(this);
            }
        }

        public void Visit(Robot robot)
        {
            robot.FullArmorRepair();
            robot.FullCoreRecharge();
            Visit((Item)robot);
        }

        public void Visit(RobotInventory inventory)
        {
            inventory.SetMaxHealth();
        }

        public static void Repair(Item item)
        {
            item.AcceptVisitor(_default);
        }
    }
}