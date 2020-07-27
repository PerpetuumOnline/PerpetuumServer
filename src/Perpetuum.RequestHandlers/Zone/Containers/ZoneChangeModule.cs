using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting.Characters;
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

        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var player = GetPlayer(request, character);

                CheckPlayerState(player);

                var container = GetContainer(request, player);

                CheckContainer(player, container);

                EquipChange(request, player, container, character);

                SendClientMessageOncomplete(request, player, container);

                scope.Complete();
            }
        }

        private Player GetPlayer(IZoneRequest request, Character character)
        {
            return request.Zone.GetPlayerOrThrow(character);
        }

        private Container GetContainer(IZoneRequest request, Player player)
        {
            var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
            return request.Zone.FindContainerOrThrow(player, containerEid);
        }

        private void CheckPlayerState(Player player)
        {
            CheckPvpState(player).ThrowIfError();
            CheckCombatState(player).ThrowIfError();
            CheckActiveModules(player).ThrowIfError();
        }

        private void CheckContainer(Player player, Container container)
        {
            CheckContainerType(container).ThrowIfError();
            CheckFieldTerminalRange(player, container).ThrowIfError();
        }

        private void EquipChange(IZoneRequest request, Player player, Container container, Character character)
        {
            // Enlist transactions on Entities
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

            // Save changes to entities
            player.Save();
            container.Save();
        }

        private void SendClientMessageOncomplete(IZoneRequest request, Player player, Container container)
        {
            var message = BuildMessage(request, player, container);
            Transaction.Current.OnCompleted(completed => OnCompleted(player, message));
        }

        private MessageBuilder BuildMessage(IZoneRequest request, Player player, Container container)
        {
            var result = new Dictionary<string, object>
                {
                    { k.robot, player.ToDictionary() },
                    { k.container, container.ToDictionary() }
                };
            return Message.Builder.FromRequest(request).WithData(result).WrapToResult();
        }

        private static void OnCompleted(Player player, MessageBuilder message)
        {
            player.SendRefreshUnitPacket();
            message.Send();
        }
    }
}