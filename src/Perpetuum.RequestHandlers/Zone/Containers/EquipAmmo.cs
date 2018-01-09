using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class EquipAmmo : ZoneContainerRequestHandler
    {
        private readonly IEntityRepository _entityRepository;

        public EquipAmmo(IEntityRepository entityRepository)
        {
            _entityRepository = entityRepository;
        }
      
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);

                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var container = request.Zone.FindContainerOrThrow(player, containerEid);

                CheckContainerType(container).ThrowIfError();
                CheckPvpState(player).ThrowIfError();
                CheckCombatState(player).ThrowIfError();
                CheckActiveModules(player).ThrowIfError();
                CheckFieldTerminalRange(player, container).ThrowIfError();

                var moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
                var module = player.GetModule(moduleEid).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);
                module.IsAmmoable.ThrowIfFalse(ErrorCodes.AmmoNotRequired);

                var ammoEid = request.Data.GetOrDefault<long>(k.ammoEID);
                var ammo = (Ammo)container.GetItemOrThrow(ammoEid);

                //check ammo type
                module.CheckLoadableAmmo(ammo.Definition).ThrowIfFalse(ErrorCodes.InvalidAmmoDefinition);
                module.UnequipAmmoToContainer(container);

                ammo = (Ammo)ammo.Unstack(module.AmmoCapacity);
                module.SetAmmo(ammo);

                player.Initialize(character);
                module.Save();
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