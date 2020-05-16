using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    /// <summary>
    /// DB lookup interface and factory for INpcReinforcements
    /// </summary>
    public interface INpcReinforcementsRepository
    {
        INpcReinforcements CreateOreNPCSpawn(MaterialType materialType, int zoneId);
        INpcReinforcements CreateNpcBossAddSpawn(NpcBossInfo npcBossInfo, int zoneId);
    }
}
