namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    /// <summary>
    /// Represents a single npc presence and its spawned state
    /// </summary>
    public interface INpcReinforcementWave
    {
        int Presence { get; }
        double Threshold { get; }
        bool Spawned { get; set; }
    }
}
