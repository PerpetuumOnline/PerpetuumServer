using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    public interface IOreNPCSpawn
    {
        int GetPresenceFor(double percentageOfFieldConsumed);
    }
}
