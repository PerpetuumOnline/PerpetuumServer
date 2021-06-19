using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using System;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    /// <summary>
    /// A NormalFlock that is the child of a RoamingPresence
    /// NormalFlocks respawn members on the Update call
    /// This class prevents that and allows the IRoamingPresence SpawnState to handle respawning all flocks.
    /// </summary>
    public class RoamingFlock : NormalFlock
    {
        public RoamingFlock(IFlockConfiguration configuration, Presence presence) : base(configuration, presence) { }

        public override void Update(TimeSpan time)
        {
            if (IsPresenceInSpawningState())
                return;

            base.Update(time);
        }

        private bool IsPresenceInSpawningState()
        {
            if (Presence is IRoamingPresence roaming)
            {
                return roaming.StackFSM.Current is SpawnState;
            }
            return false;
        }

        protected override Position GetSpawnPosition(Position spawnOrigin)
        {
            if (Presence is IRoamingPresence roaming)
            {
                return roaming.SpawnOrigin.Clamp(Presence.Zone.Size);
            }
            return base.GetSpawnPosition(spawnOrigin);
        }
    }
}
