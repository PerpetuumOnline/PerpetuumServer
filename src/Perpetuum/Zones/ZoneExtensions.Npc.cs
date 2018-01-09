using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

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
    }
}
