using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones
{
    public static partial class ZoneExtensions
    {
        public static IEnumerable<Character> GetCharacters(this IZone zone)
        {
            return zone.Players.Select(p => p.Character);
        }

        public static void SendMessageToPlayers(this IZone zone, MessageBuilder builder)
        {
            if ( zone == null )
                return;

            builder.ToCharacters(zone.GetCharacters()).Send();
        }

        public static Player GetPlayerOrThrow(this IZone zone, Character character)
        {
            return zone.GetPlayer(character).ThrowIfNull(ErrorCodes.PlayerNotFound);
        }

        public static bool TryGetPlayer(this IZone zone, Character character, out Player player)
        {
            player = zone.GetPlayer(character);
            return player != null;
        }

        [CanBeNull]
        public static Player GetPlayer(this IZone zone, Character character)
        {
            return zone.Players.FirstOrDefault(p => p.Character == character);
        }

        [CanBeNull]
        public static Player GetPlayerByCharacterId(this IZone zone, int characterId)
        {
            return zone.Players.FirstOrDefault(p => p.Character.Id == characterId);
        }

        [CanBeNull]
        public static Player ToPlayerOrGetOwnerPlayer(this IZone zone,Unit unit)
        {
            if (zone == null || unit == null)
                return null;

            var player = unit as Player;
            if (player != null)
                return player;

            return zone.GetPlayer(unit.GetOwnerAsCharacter());
        }

        public static Position GetPrimaryLockedTileOrThrow(this IZone zone, Character character, bool centerOfTile = true)
        {
            var player = zone.GetPlayerOrThrow(character);

            var tl = player.GetPrimaryLock().ThrowIfNotType<TerrainLock>(ErrorCodes.PrimaryLockTargetNotFound);
            var position = tl.Location;

            if (centerOfTile)
            {
                position = position.Center;
            }

            return position;
        }
    }
}
