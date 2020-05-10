using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    /// <summary>
    /// Ore NPC spawn data that tracks spawned state for some Presence, Threshold, OreType tuple
    /// </summary>
    public interface IOreNpcData
    {
        MaterialType OreType { get; }
        int Presence { get; }
        double Threshold { get; }
        bool Spawned { get; set; }
    }
}
