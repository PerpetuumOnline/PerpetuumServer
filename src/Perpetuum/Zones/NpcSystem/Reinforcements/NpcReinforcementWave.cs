using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcementWave : INpcReinforcementWave
    {
        public int Presence { get; private set; }
        public double Threshold { get; private set; }
        public bool Spawned { get; set; }
        public NpcReinforcementWave(int presenceID, double threshold)
        {
            Presence = presenceID;
            Threshold = threshold;
        }

        public override string ToString()
        {
            return $"{Threshold}:{Presence} Spawned? {Spawned}";
        }
    }
}
