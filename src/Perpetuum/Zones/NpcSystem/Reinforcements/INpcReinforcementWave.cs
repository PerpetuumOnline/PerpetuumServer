using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    /// <summary>
    /// Represents a single npc presence and its spawned state
    /// </summary>
    public interface INpcReinforcementWave
    {
        int PresenceId { get; }
        DynamicPresence ActivePresence { get; set; }
        double Threshold { get; }
        bool Spawned { get; }

        /// <summary>
        /// Test if the provided presence is the ActivePresence of this wave
        /// </summary>
        /// <param name="presence">Presence to compare</param>
        /// <returns>true if reference is equal</returns>
        bool IsActivePresence(Presence presence);

        /// <summary>
        /// Deactivate ActivePresence
        /// </summary>
        void DeactivatePresence();
    }
}
