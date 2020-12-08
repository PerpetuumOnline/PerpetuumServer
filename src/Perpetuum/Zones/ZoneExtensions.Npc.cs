using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;
using System;

namespace Perpetuum.Zones
{
    public static partial class ZoneExtensions
    {
        public static DynamicPresence AddDynamicPresenceToPosition(this IZone zone,int presenceID, Position position)
        {
            var presence = zone.PresenceManager.CreatePresence(presenceID).ThrowIfNotType<DynamicPresence>(ErrorCodes.ItemNotFound);
            presence.DynamicPosition = position;
            presence.LoadFlocks();
            presence.Flocks.SpawnAllMembers();
            zone.PresenceManager.AddPresence(presence);
            return presence;
        }

        public static DynamicPresenceExtended AddDynamicPresenceToPosition(this IZone zone, int presenceID, Position homePos, Position startPos, TimeSpan timeSpan)
        {
            var presence = zone.PresenceManager.CreatePresence(presenceID).ThrowIfNotType<DynamicPresenceExtended>(ErrorCodes.ItemNotFound);
            presence.LifeTime = timeSpan;
            presence.DynamicPosition = homePos;
            presence.SpawnLocation = startPos;
            presence.LoadFlocks();
            presence.Flocks.SpawnAllMembers();
            zone.PresenceManager.AddPresence(presence);
            return presence;
        }
    }
}
