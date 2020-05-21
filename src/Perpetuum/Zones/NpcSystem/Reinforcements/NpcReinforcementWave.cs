using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcementWave : INpcReinforcementWave
    {
        public int PresenceId { get; }
        public double Threshold { get; }
        public bool Spawned => ActivePresence != null;
        public DynamicPresence ActivePresence { get; set; }

        public NpcReinforcementWave(int presenceID, double threshold)
        {
            PresenceId = presenceID;
            Threshold = threshold;
        }

        public override string ToString()
        {
            return $"{Threshold}:{PresenceId} Spawned? {Spawned}";
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
