using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class EquipModule : IRequestHandler
    {
        private readonly IEntityRepository _entityRepository;
        private readonly RobotHelper _robotHelper;

        public EquipModule(IEntityRepository entityRepository,RobotHelper robotHelper)
        {
            _entityRepository = entityRepository;
            _robotHelper = robotHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var container = Container.GetWithItems(containerEid, character).ThrowIfNull(ErrorCodes.ContainerNotFound);
                container.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.AccessDenied);

                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var robot = _robotHelper.LoadRobotOrThrow(robotEid);
                robot.IsSingleAndUnpacked.ThrowIfFalse(ErrorCodes.RobotMustbeSingleAndNonRepacked);
                robot.Initialize(character);

                var slot = request.Data.GetOrDefault<int>(k.slot);
                var componentType = request.Data.GetOrDefault<string>(k.robotComponent).ToEnum<RobotComponentType>();
                var component = robot.GetRobotComponentOrThrow(componentType);
                component.MakeSlotFree(slot, container);
                var moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
                var module = (Module)container.GetItemOrThrow(moduleEid).Unstack(1);
                component.EquipModuleOrThrow(module, slot);

                robot.Initialize(character);
                robot.Save();
                container.Save();

                Transaction.Current.OnCompleted(completed =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.robot, robot.ToDictionary()}, 
                        {k.container, container.ToDictionary()}
                    };
                    Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                });
                
                scope.Complete();
            }
        }
    }
}