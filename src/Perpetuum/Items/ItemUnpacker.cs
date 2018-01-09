using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.Robots;

namespace Perpetuum.Items
{
    public class ItemUnpacker : IEntityVisitor<Item>,IEntityVisitor<Robot>,IEntityVisitor<VolumeWrapperContainer>
    {
        private static readonly ItemUnpacker _default = new ItemUnpacker();

        public void Visit(Item item)
        {
            item.IsRepackaged = false;
        }

        public void Visit(Robot robot)
        {
            var parentEntity = robot.GetOrLoadParentEntity();
            //egy olyan parentbol ami robotInventory = masik robot gyomraban van?
            parentEntity.ThrowIfType<RobotInventory>(ErrorCodes.CannotUnpackRobotInRobotInventory);
            //egy olyan parentbol ami volumewrapper
            parentEntity.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.CannotUnpackRobotInVolumeWrapper);

            robot.IsRepackaged = false;
            // osszerakjuk a darabokat, majd a SaveToDb inserttel

            robot.CreateComponents();
            robot.Owner = robot.Owner;

            robot.Repair();
            robot.Initialize(robot.GetCharacter());
        }

        public void Visit(VolumeWrapperContainer container)
        {
            container.SetRandomName();
            container.PrincipalCharacter = container.GetOwnerAsCharacter();
            container.IsRepackaged = false;
        }

        public static void Unpack(Item item)
        {
            item.IsRepackaged.ThrowIfFalse(ErrorCodes.ItemIsNotPackaged);
            item.AcceptVisitor(_default);
        }

    }
}