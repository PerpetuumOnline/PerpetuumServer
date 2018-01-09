using System;
using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    public class FlockConfiguration : IFlockConfiguration
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int PresenceID { get; set; }
        public int FlockMemberCount { get; set; }
        public Position SpawnOrigin { get; set; }
        public IntRange SpawnRange { get; set; }
        public int TotalSpawnCount { get; set; }
        public string Note { get; set; }
        public double RespawnMultiplierLow { get; set; }
        public bool IsCallForHelp { get; set; }
        public bool Enabled { get; set; }
        public NpcBehaviorType BehaviorType { get; set; }
        public int HomeRange { get; set; }
        public EntityDefault EntityDefault { get; set; }
        public TimeSpan RespawnTime { get; set; }

        public FlockConfiguration()
        {
            BehaviorType = NpcBehaviorType.Neutral;
            HomeRange = (int) DistanceConstants.MAX_NPC_FLOCK_HOME_RANGE;
        }

        public override string ToString()
        {
            return $"{ID}:{PresenceID}:{Name}";
        }
    }
}
