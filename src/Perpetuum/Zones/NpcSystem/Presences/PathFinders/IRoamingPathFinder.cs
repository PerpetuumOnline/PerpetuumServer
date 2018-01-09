using System.Drawing;

namespace Perpetuum.Zones.NpcSystem.Presences.PathFinders
{
    public interface IRoamingPathFinder
    {
        Point FindSpawnPosition(RoamingPresence presence);
        Point FindNextRoamingPosition(RoamingPresence presence);
    }
}