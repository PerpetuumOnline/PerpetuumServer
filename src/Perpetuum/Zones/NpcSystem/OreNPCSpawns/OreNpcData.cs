using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    public class OreNpcData : IOreNpcData
    {
        public MaterialType OreType { get; private set; }
        public int Presence { get; private set; }
        public double Threshold { get; private set; }
        public bool Spawned { get; set; }
        public OreNpcData(MaterialType oreType, int presenceID, double threshold)
        {
            OreType = oreType;
            Presence = presenceID;
            Threshold = threshold;
        }

        public override string ToString()
        {
            return $"{Threshold}:{Presence} Spawned? {Spawned}";
        }
    }
}
