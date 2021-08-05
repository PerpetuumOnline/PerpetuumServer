using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence
{
    public class StaticExpiringFlock : RoamingFlock
    {
        public StaticExpiringFlock(IFlockConfiguration configuration, Presence presence) : base(configuration, presence) { }

        protected override bool IsPresenceInSpawningState()
        {
            if (Presence is RandomSpawningExpiringPresence pres)
            {
                return pres.StackFSM.Current is StaticSpawnState;
            }
            return base.IsPresenceInSpawningState();
        }

        protected override Position GetSpawnPosition(Position spawnOrigin)
        {
            if (Presence is RandomSpawningExpiringPresence pres)
            {
                spawnOrigin = pres.SpawnOrigin.Clamp(Presence.Zone.Size);
            }
            return base.GetSpawnPosition(spawnOrigin);
        }
    }
}
