using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Presences.InterzonePresences;
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

        public static DynamicPresenceExtended AddDynamicPresenceToPosition(this IZone zone, int presenceID, Position position, TimeSpan timeSpan)
        {
            var presence = zone.PresenceManager.CreatePresence(presenceID).ThrowIfNotType<DynamicPresenceExtended>(ErrorCodes.ItemNotFound);
            presence.LifeTime = timeSpan;
            presence.DynamicPosition = position;
            presence.LoadFlocks();
            presence.Flocks.SpawnAllMembers();
            zone.PresenceManager.AddPresence(presence);
            return presence;
        }
    }
}
