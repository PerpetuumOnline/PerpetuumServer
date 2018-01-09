using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public static class PresenceExtensions
    {
        [NotNull]
        public static Presence GetPresenceOrThrow(this IEnumerable<Presence> presences, int presenceId)
        {
            return presences.FirstOrDefault(p => p.Configuration.ID == presenceId).ThrowIfNull(ErrorCodes.PresenceNotFound);
        }
    }
}