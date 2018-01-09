using System.Linq;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Robots;
using Perpetuum.Services.Insurance;

namespace Perpetuum.Items
{
    public class ItemPacker : IEntityVisitor<Item>,
                              IEntityVisitor<Robot>,
                              IEntityVisitor<RobotInventory>,
                              IEntityVisitor<RobotComponent>,
                              IEntityVisitor<Container>,
                              IEntityVisitor<VolumeWrapperContainer>
    {
        private static readonly ItemPacker _default = new ItemPacker();

        private static void PackItem(Item item)
        {
            item.DynamicProperties.Clear();
            item.IsRepackaged = true;
            item.Name = null;
        }

        public void Visit(Item item)
        {
            item.IsRepackaged.ThrowIfTrue(ErrorCodes.ItemHasToBeUnpacked);
            item.ED.AttributeFlags.Repackable.ThrowIfFalse(ErrorCodes.NothingToDo);
            item.IsDamaged.ThrowIfTrue(ErrorCodes.ItemHasToBeRepaired);

            PackItem(item);
        }

        public void Visit(Robot robot)
        {
            robot.IsDamaged.ThrowIfTrue(ErrorCodes.RobotHasToBeRepaired);
            robot.IsSelected.ThrowIfTrue(ErrorCodes.RobotMustBeDeselected);
            InsuranceHelper.IsInsured(robot.Eid).ThrowIfTrue(ErrorCodes.ItemIsInsuredOperationFails);

            robot.VisitRobotComponents(this);
            robot.VisitRobotInventory(this);

            foreach (var component in robot.Components)
            {
                Entity.Repository.Delete(component);
            }

            PackItem(robot);
        }

        public void Visit(RobotInventory inventory)
        {
            inventory.GetItems().Any().ThrowIfTrue(ErrorCodes.RobotHasItemsInContainer);
        }

        public void Visit(RobotComponent component)
        {
            if (component.Modules.Any())
                throw new PerpetuumException(ErrorCodes.RobotHasModulesEquipped);
        }

        public void Visit(Container container)
        {
            container.ThrowIfType<SystemContainer>(ErrorCodes.ItemNotPackable);
            container.ThrowIfType<PublicContainer>(ErrorCodes.ItemNotPackable);
            container.ThrowIfType<PublicCorporationHangarStorage>(ErrorCodes.ItemNotPackable);
            container.ThrowIfType<CorporateHangar>(ErrorCodes.ItemNotPackable);
            container.ThrowIfType<CorporateHangarFolder>(ErrorCodes.ItemNotPackable);

            container.GetItems().Any().ThrowIfTrue(ErrorCodes.ContainerHasToBeEmpty);
            Visit((Item)container);
        }

        public void Visit(VolumeWrapperContainer container)
        {
            container.IsInAssignment().ThrowIfTrue(ErrorCodes.ContainerInAssignment);

            Visit((Container)container);

            container.ClearAssignmentId();
        }

        public static void Pack(Item item)
        {
            item.AcceptVisitor(_default);
        }
    }
}