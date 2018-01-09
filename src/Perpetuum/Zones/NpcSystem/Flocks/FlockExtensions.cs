using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    public static class FlockExtensions
    {
        public static void SpawnAllMembers(this IEnumerable<Flock> flocks)
        {
            foreach (var flock in flocks)
            {
                flock.SpawnAllMembers();
            }
        }

        public static IEnumerable<Npc> GetMembers(this IEnumerable<Flock> flocks)
        {
            return flocks.SelectMany(f => f.Members);
        }

        public static int MembersCount(this IEnumerable<Flock> flocks)
        {
            return flocks.Sum(f => f.MembersCount);
        }

        [NotNull]
        public static Flock GetFlockOrThrow(this IEnumerable<Flock> flocks, int flockId)
        {
            return flocks.GetFlock(flockId).ThrowIfNull(ErrorCodes.FlockNotFound);
        }

        [CanBeNull]
        public static Flock GetFlock(this IEnumerable<Flock> flocks, int flockId)
        {
            return flocks.FirstOrDefault(f => f.Id == flockId);
        }
    }
}