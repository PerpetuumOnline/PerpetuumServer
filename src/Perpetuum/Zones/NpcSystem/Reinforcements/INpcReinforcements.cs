using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    /// <summary>
    /// Ore NPC Spawn - a list of presences that spawn at particular percentages of some activity completion
    /// </summary>
    public interface INpcReinforcements
    {
        /// <summary>
        /// Gets the next IOreNpcData for the provided threshold percent mined
        /// </summary>
        /// <param name="threshold">double of range [0.-1.] representing a percentage</param>
        /// <returns>IOreNpcData</returns>
        INpcReinforcementWave GetNextPresence(double threshold);

        /// <summary>
        /// Does this INpcReinforcements have the provided Presence active?
        /// </summary>
        /// <param name="presence">Presence to compare</param>
        /// <returns>true if provided presence is spawned in this INpcReinforcements</returns>
        bool HasActivePresence(Presence presence);

        /// <summary>
        /// Get the INpcReinforcementWave of some active Presence
        /// </summary>
        /// <param name="presence">Presence to compare</param>
        /// <returns>INpcReinforcementWave of spawned presence</returns>
        INpcReinforcementWave GetActiveWaveOfPresence(Presence presence);

        /// <summary>
        /// Get all active waves (waves with active presence)
        /// </summary>
        /// <returns>INpcReinforcementWaves with active presences</returns>
        INpcReinforcementWave[] GetAllActiveWaves();
    }
}
