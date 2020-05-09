using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    public interface IOreNPCRepository
    {
        int GetPresenceForOreAndThreshold(MaterialType materialType, double threshold);
    }
}
