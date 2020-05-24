using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    /// <summary>
    /// Represents a single npc presence and its spawned state
    /// </summary>
    public interface INpcReinforcementWave
    {
        int PresenceId { get; }
        DynamicPresence ActivePresence { get; }
        double Threshold { get; }
        bool Spawned { get; }

        /// <summary>
        /// Set the ActivePresence and mark this wave as "Spawned"
        /// </summary>
        /// <param name="presence">presence that is spawned to the zone</param>
        void SetActivePresence(DynamicPresence presence);

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
