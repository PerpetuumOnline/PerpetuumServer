using System.Drawing;

namespace Perpetuum.Zones.NpcSystem.Presences.PathFinders
{
    public interface IRoamingPathFinder
    {
        Point FindSpawnPosition(IRoamingPresence presence);
        Point FindNextRoamingPosition(IRoamingPresence presence);
    }
}