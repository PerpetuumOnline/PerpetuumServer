using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    /// <summary>
    /// DB lookup interface and factory for IOreNPCSpawns
    /// </summary>
    public interface IOreNpcRepository
    {
        IOreNpcSpawn CreateOreNPCSpawn(MaterialType materialType);
    }
}
