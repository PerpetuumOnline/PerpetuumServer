using Perpetuum.Data;
using Perpetuum.Players;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using System;

namespace Perpetuum.Services.Strongholds
{
    public interface IStrongholdPlayerStateManager
    {
        void OnPlayerEnterZone(Player player);
        void OnPlayerExitZone(Player player);
    }

    public class StrongholdPlayerStateManager : IStrongholdPlayerStateManager
    {
        /// <summary>
        /// Handle player-stronghold state.
        /// If the player is new to a stronghold zone, set the expiration
        /// If the player is returning to the same stronghold zone (from logoff) apply existing expiration
        /// </summary>
        /// <param name="zone">Zone the player is added to</param>
        /// <param name="player">The Player being added to zone</param>
        public static void OnPlayerAddToZone(Zone zone, Player player)
        {
            UpdatePlayerState(player, (p) =>
            {
                if (!IsSameZone(p, zone))
                {
                    p.DynamicProperties.Remove(k.strongholdDespawnTime);
                }
                zone.PlayerStateManager?.OnPlayerEnterZone(p);
                p.DynamicProperties.Update(k.prevZone, zone.Id);
            });
        }

        private readonly IZone _zone;
        private readonly TimeSpan MAX = TimeSpan.FromMinutes(60);
        private readonly TimeSpan MIN = TimeSpan.FromSeconds(30);

        private readonly EventListenerService _eventChannel;

        public StrongholdPlayerStateManager(IZone zone, EventListenerService eventChannel)
        {
            _eventChannel = eventChannel;
            _zone = zone;
            MAX = TimeSpan.FromMinutes(_zone.Configuration.TimeLimitMinutes ?? 60);
        }

        public static bool IsSameZone(Player player, IZone zone)
        {
            return player.DynamicProperties.GetOrDefault<int>(k.prevZone) == zone.Id;
        }

        public void OnPlayerEnterZone(Player player)
        {
            var now = DateTime.UtcNow;
            if (!IsSameZone(player, _zone))
            {
                player.DynamicProperties.Remove(k.strongholdDespawnTime);
            }
            var effectEnd = player.DynamicProperties.GetOrAdd(k.strongholdDespawnTime, now.Add(MAX));
            var effectDuration = (effectEnd - now).Max(MIN);
            ApplyDespawn(player, effectDuration, effectEnd);
            SendEntryMessage(player, effectDuration);
        }

        public void OnPlayerExitZone(Player player)
        {
            player.ClearStrongholdDespawn();
        }

        private void ApplyDespawn(Player player, TimeSpan remaining, DateTime endTime)
        {
            player.DynamicProperties.Update(k.strongholdDespawnTime, endTime);
            player.SetStrongholdDespawn(remaining, (u) =>
            {
                if (u is Player p)
                {
                    UpdatePlayerState(p, DoDespawnAction);
                }
            });
        }

        private void DoDespawnAction(Player player)
        {
            var dockingBase = player.Character.GetHomeBaseOrCurrentBase();
            player.DockToBase(dockingBase.Zone, dockingBase);
            player.DynamicProperties.Remove(k.prevZone);
            player.DynamicProperties.Remove(k.strongholdDespawnTime);
            SendRemovalMessage(player);
        }

        private static void UpdatePlayerState(Player player, Action<Player> transaction)
        {
            using (var scope = Db.CreateTransaction())
            {
                transaction(player);
                player.Save();
                scope.Complete();
            }
        }

        private void SendEntryMessage(Player player, TimeSpan effectDuration)
        {
            var message = $"You only have {effectDuration.ToHumanTimeString()} remaining on this island before the Syndicate will teleport you home.\nGood luck!";
            SendMessage(player, message);
        }

        private void SendRemovalMessage(Player player)
        {
            var message = $"You are back safely!\nThe Syndicate had to pull you out or risk losing your connection to those pesky Nians.";
            SendMessage(player, message);
        }

        private void SendMessage(Player player, string message)
        {
            _eventChannel.PublishMessage(new DirectMessage(player.Character, message));
        }
    }
}
