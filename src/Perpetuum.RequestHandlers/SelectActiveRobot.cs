using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class SelectActiveRobot : IRequestHandler
    {
        private readonly IEntityRepository _entityRepository;
        private readonly RobotHelper _robotHelper;

        public SelectActiveRobot(IEntityRepository entityRepository,RobotHelper robotHelper)
        {
            _entityRepository = entityRepository;
            _robotHelper = robotHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
                character.IsRobotSelectedForOtherCharacter(robotEid).ThrowIfTrue(ErrorCodes.UnknownError);

                var container = Container.GetOrThrow(containerEid);
                //not possible from corp hangar, robot inventory, and system container
                container.ThrowIfType<CorporateHangar>(ErrorCodes.AccessDenied);
                container.ThrowIfType<CorporateHangarFolder>(ErrorCodes.AccessDenied);
                container.ThrowIfType<RobotInventory>(ErrorCodes.AccessDenied);
                container.ThrowIfType<DefaultSystemContainer>(ErrorCodes.AccessDenied);
                container.CheckAccessAndThrowIfFailed(character, ContainerAccess.List);

                var robot = _robotHelper.LoadRobotForCharacter(robotEid, character,true);
                robot.Parent.ThrowIfNotEqual(containerEid, ErrorCodes.ParentError);
                robot.IsRepackaged.ThrowIfTrue(ErrorCodes.ItemHasToBeUnpacked);

                robot.CheckEnablerExtensionsAndThrowIfFailed(character);
                character.SetActiveRobot(robot);
                robot.Save();

                Message.Builder.FromRequest(request).WithOk().Send();
                scope.Complete();
            }
        }
    }
}
