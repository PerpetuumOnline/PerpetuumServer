using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class UnequipAmmo : ZoneContainerRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);
                var container = request.Zone.FindContainerOrThrow(player, containerEid);

                CheckContainerType(container).ThrowIfError();
                CheckPvpState(player).ThrowIfError();
                CheckCombatState(player).ThrowIfError();
                CheckActiveModules(player).ThrowIfError();
                CheckFieldTerminalRange(player, container).ThrowIfError();

                player.EnlistTransaction();
                container.EnlistTransaction();

                var moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
                var module = player.GetModule(moduleEid).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);
                module.IsAmmoable.ThrowIfFalse(ErrorCodes.AmmoNotRequired);
                var ammo = module.UnequipAmmoToContainer(container);
                if (ammo != null)
                    ammo.Owner = character.Eid;

                player.Initialize(character);
                player.Save();
                container.Save();

                Transaction.Current.OnCompleted(completed =>
                {
                    player.SendRefreshUnitPacket();

                    var result = new Dictionary<string, object>
                    {
                        {k.robot, player.ToDictionary()}, 
                        {k.container, container.ToDictionary()}
                    };
                    Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                });
                
                scope.Complete();
            }
        }
    }
}