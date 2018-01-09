using System;

namespace Perpetuum.Zones.Terrains
{
    [Flags]
    public enum BlockingFlags : byte
    {
        Undefined = 0,
        Obstacle = 1,
        Plant = 1 << 1,
        Decor = 1 << 2,
        Island = 1 << 3,

        NonNaturally = Decor | Island | Obstacle
    }
}