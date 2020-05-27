using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcementWave : INpcReinforcementWave
    {
        public int PresenceId { get; }
        public double Threshold { get; }
        public bool Spawned { get; private set; }
        public DynamicPresence ActivePresence { get; private set; }

        public NpcReinforcementWave(int presenceID, double threshold)
        {
            PresenceId = presenceID;
            Threshold = threshold;
        }

        public override string ToString()
        {
            return $"{Threshold}:{PresenceId} Spawned? {Spawned}";
        }

        public void SetActivePresence(DynamicPresence presence)
        {
            ActivePresence = presence;
            Spawned = true;
        }

        public bool IsActivePresence(Presence presence)
        {
            return ReferenceEquals(ActivePresence, presence);
        }

        public void DeactivatePresence()
        {
            ActivePresence = null;
        }
    }
}
