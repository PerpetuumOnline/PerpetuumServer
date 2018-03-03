using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Robots;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class ZoneChangeModule : ZoneContainerRequestHandler
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

                var componentType = request.Data.GetOrDefault<string>(k.sourceComponent).ToEnum<RobotComponentType>();
                var component = player.GetRobotComponentOrThrow(componentType);
                var sourceSlot = request.Data.GetOrDefault<int>(k.source);
                var targetSlot = request.Data.GetOrDefault<int>(k.target);
                component.ChangeModuleOrThrow(sourceSlot, targetSlot);

                player.Initialize(character);

                player.Save();

                Transaction.Current.OnCommited(() =>
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