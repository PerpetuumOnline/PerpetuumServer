namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    /// <summary>
    /// Ore NPC Spawn - a list of presences that spawn at particular levels of field depletion
    /// </summary>
    public interface IOreNpcSpawn
    {
        /// <summary>
        /// Gets the next IOreNpcData for the provided threshold percent mined
        /// </summary>
        /// <param name="percentageOfFieldConsumed">double of range [0.-1.] representing a percentage of ore field mined</param>
        /// <returns>IOreNpcData</returns>
        IOreNpcData GetNextPresence(double percentageOfFieldConsumed);
    }
}
