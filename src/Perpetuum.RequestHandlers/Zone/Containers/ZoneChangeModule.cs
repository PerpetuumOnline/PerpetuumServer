using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public abstract class ZoneChangeModule : ZoneContainerRequestHandler
    {
        /// <summary>
        /// Equip or Remove module based on implementing class
        /// Must be called inside of transaction in HandleRequest
        /// </summary>
        public abstract void DoChange(IZoneRequest request, Player player, Container container);

        private static void OnCompleted(Player player, MessageBuilder message)
        {
            player.SendRefreshUnitPacket();
            message.Send();
        }

        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);

                CheckPvpState(player).ThrowIfError();
                CheckCombatState(player).ThrowIfError();
                CheckActiveModules(player).ThrowIfError();

                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var container = request.Zone.FindContainerOrThrow(player, containerEid);

                CheckContainerType(container).ThrowIfError();
                CheckFieldTerminalRange(player, container).ThrowIfError();

                player.EnlistTransaction();
                container.EnlistTransaction();

                // Store ratios before transaction
                var hpRatio = player.ArmorPercentage;
                var apRatio = player.CorePercentage;

                // Call equip/remove
                DoChange(request, player, container);
                player.OnEquipChange();

                player.Initialize(character);
                player.CheckEnergySystemAndThrowIfFailed();

                // Apply ratios to prevent resetting values
                player.Armor = player.ArmorMax * hpRatio;
                player.Core = player.CoreMax * apRatio;


                player.Save();
                container.Save();

                var result = new Dictionary<string, object> {
                    { k.robot, player.ToDictionary() },
                    { k.container, container.ToDictionary() }
                };
                var message = Message.Builder.FromRequest(request).WithData(result).WrapToResult();
                Transaction.Current.OnCompleted(completed => OnCompleted(player, message));

                scope.Complete();
            }
        }
    }
}