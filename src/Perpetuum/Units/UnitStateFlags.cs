using System;

namespace Perpetuum.Units
{
    /// <summary>
    /// Controls the state of any unit on the zone
    /// </summary>
    [Flags, Serializable]
    public enum UnitStateFlags : ulong
    {
        undefined = 0,
        reinforced = 1,
        aggressive = 1 << 1,
        flying = 1 << 2,
        dock = 1 << 25,
        teleport = 1 << 26,
        dead = 1 << 27,
        combat = 1 << 28,
        mineral_scan = (ulong)1 << 33,
        lock_something = (ulong)1 << 34,
        local_teleport = (ulong)1 << 35,
        useEnabled = (ulong)1 << 36,
        inMovable = (ulong)1 << 37,
        damage_level75 = (ulong)1 << 38,
        damage_level50 = (ulong)1 << 39,
        damage_level25 = (ulong)1 << 40,
        unlockable = (ulong)1 << 41,
        bigbrother = (ulong)1 << 43,
        isOnline = (ulong)1 << 44,
        isOrphaned = (ulong)1 << 45,
    }
}