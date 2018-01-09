using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class EquipAmmo : IRequestHandler
    {
        private readonly IEntityRepository _entityRepository;
        private readonly RobotHelper _robotHelper;

        public EquipAmmo(IEntityRepository entityRepository,RobotHelper robotHelper)
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
                var container = Container.GetWithItems(containerEid, character);
                container.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.AccessDenied);

                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var robot = _robotHelper.LoadRobotOrThrow(robotEid);
                robot.IsSingleAndUnpacked.ThrowIfFalse(ErrorCodes.RobotMustbeSingleAndNonRepacked);

                var moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
                var module = robot.GetModule(moduleEid).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);
                module.IsAmmoable.ThrowIfFalse(ErrorCodes.AmmoNotRequired);

                var ammoEid = request.Data.GetOrDefault<long>(k.ammoEID);
                var ammo = (Ammo)container.GetItemOrThrow(ammoEid);

                //check ammo type
                module.CheckLoadableAmmo(ammo.Definition).ThrowIfFalse(ErrorCodes.InvalidAmmoDefinition);
                module.UnequipAmmoToContainer(container);

                ammo = (Ammo)ammo.Unstack(module.AmmoCapacity);
                module.SetAmmo(ammo);

                robot.Initialize(character);
                module.Save();
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