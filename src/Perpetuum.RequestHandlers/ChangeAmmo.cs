using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class ChangeAmmo : IRequestHandler
    {
        private readonly IEntityRepository _entityRepository;
        private readonly RobotHelper _robotHelper;

        public ChangeAmmo(IEntityRepository entityRepository,RobotHelper robotHelper)
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

                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var robot = _robotHelper.LoadRobotOrThrow(robotEid);
                robot.IsSingleAndUnpacked.ThrowIfFalse(ErrorCodes.RobotMustbeSingleAndNonRepacked);
                robot.EnlistTransaction();

                var sourceModuleEid = request.Data.GetOrDefault<long>(k.sourceModuleEID);
                var sourceModule = robot.GetModule(sourceModuleEid).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);
                sourceModule.IsAmmoable.ThrowIfFalse(ErrorCodes.AmmoNotRequired);

                var sourceAmmo = sourceModule.GetAmmo().ThrowIfNull(ErrorCodes.AmmoNotFound);

                var targetModuleEid = request.Data.GetOrDefault<long>(k.targetModuleEID);
                var targetModule = robot.GetModule(targetModuleEid).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);
                targetModule.IsAmmoable.ThrowIfFalse(ErrorCodes.AmmoNotRequired);
                targetModule.CheckLoadableAmmo(sourceAmmo.Definition).ThrowIfFalse(ErrorCodes.InvalidAmmoDefinition);
                targetModule.AmmoCapacity.ThrowIfLess(sourceAmmo.Quantity, ErrorCodes.ModuleOutOfSpace);

                var targetAmmo = targetModule.GetAmmo();
                if (targetAmmo == null)
                {
                    //target module is empty
                    sourceModule.SetAmmo(null);
                }
                else
                {
                    //target module has ammo
                    sourceModule.CheckLoadableAmmo(targetAmmo.Definition).ThrowIfFalse(ErrorCodes.InvalidAmmoDefinition);
                    sourceModule.AmmoCapacity.ThrowIfLess(targetAmmo.Quantity, ErrorCodes.ModuleOutOfSpace);
                    sourceModule.SetAmmo(targetAmmo);
                }

                targetModule.SetAmmo(sourceAmmo);

                robot.Initialize(character);
                robot.Save();

                Transaction.Current.OnCompleted(completed =>
                {
                    var result = new Dictionary<string, object> { { k.robot, robot.ToDictionary() } };
                    Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                });
                
                scope.Complete();
            }
        }
    }
}