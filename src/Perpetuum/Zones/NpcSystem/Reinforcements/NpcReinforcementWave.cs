namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcementWave : INpcReinforcementWave
    {
        public int Presence { get; }
        public double Threshold { get; }
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
