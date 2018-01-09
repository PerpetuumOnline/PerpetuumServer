using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Robots
{
   
    /// <summary>
    /// Limited capacity inventory for a robot
    /// </summary>
    public class RobotInventory : LimitedCapacityContainer
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void AddItem(Item item,long issuerEid, bool doStack)
        {
            (item.IsCategory(CategoryFlags.cf_robots) && !item.IsRepackaged).ThrowIfTrue(ErrorCodes.ItemHasToBeRepackaged);

            if (item.IsCategory(CategoryFlags.cf_container))
            {
                item.ThrowIfNotType<VolumeWrapperContainer>(ErrorCodes.ContainersAreNotSupported);
            }

            base.AddItem(item,issuerEid, doStack);
        }

        public override void ReloadItems(long? issuerEid)
        {
            CheckParentRobotAndThrowIfFailed(issuerEid);
            base.ReloadItems(issuerEid);
        }

        public void CheckParentRobotAndThrowIfFailed(long? issuerEid)
        {
            CheckParentRobot(issuerEid).ThrowIfError();
        }

        public ErrorCodes CheckParentRobot(long? issuerEid)
        {
            var robot = GetOrLoadParentEntity() as Robot;
            if ( robot == null )
                return ErrorCodes.NoError;
                
            if (issuerEid != null && robot.Owner != issuerEid)
                return ErrorCodes.AccessDenied;

            if ( !robot.IsSingleAndUnpacked )
                return ErrorCodes.RobotMustbeSingleAndNonRepacked;

            if ( robot.IsTrashed )
                return ErrorCodes.AccessDenied;

            return ErrorCodes.NoError;
        }

        protected override void RelocateItem(Character character, long issuerEid, Item item, Container targetContainer)
        {
            targetContainer.ThrowIfType<RobotInventory>(ErrorCodes.AccessDenied);
            base.RelocateItem(character, issuerEid, item, targetContainer);
        }

        public Task SendUpdateToOwnerAsync()
        {
            return Task.Run(() =>
            {
                SendUpdateToOwner();
            });
        }

        public void SendUpdateToOwner()
        {
            var data = ToDictionary();
            Message.Builder.SetCommand(Commands.ContainerUpdate).WithData(data).ToCharacter(this.GetOwnerAsCharacter()).Send();
        }
    }
}
