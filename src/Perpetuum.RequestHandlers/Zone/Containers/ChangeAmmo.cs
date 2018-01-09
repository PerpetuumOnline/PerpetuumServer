using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class ChangeAmmo : ZoneContainerRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);

                CheckPvpState(player).ThrowIfError();
                CheckCombatState(player).ThrowIfError();
                CheckActiveModules(player).ThrowIfError();

                player.EnlistTransaction();

                var sourceModuleEid = request.Data.GetOrDefault<long>(k.sourceModuleEID);
                var sourceModule = player.GetModule(sourceModuleEid).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);
                sourceModule.IsAmmoable.ThrowIfFalse(ErrorCodes.AmmoNotRequired);

                var sourceAmmo = sourceModule.GetAmmo().ThrowIfNull(ErrorCodes.AmmoNotFound);

                var targetModuleEid = request.Data.GetOrDefault<long>(k.targetModuleEID);
                var targetModule = player.GetModule(targetModuleEid).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);
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

                player.Initialize(character);
                player.Save();

                Transaction.Current.OnCompleted(completed =>
                {
                    player.SendRefreshUnitPacket();

                    var result = new Dictionary<string, object>
                    {
                        { k.robot, player.ToDictionary() }
                    };

                    Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                });
                
                scope.Complete();
            }
        }
    }
}