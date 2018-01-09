using System;

namespace Perpetuum.Zones.Terrains
{
    [Flags]
    public enum TerrainControlFlags  : ushort
    {
        Undefined = 0,
        AntiPlant = 1,
        TerraformProtected = 1 << 1,
        SyndicateArea  = 1 << 2,
        ConcreteA = 1 << 3,
        ConcreteB = 1 << 4,
        Roaming = 1 << 6,
        Highway = 1 << 7,
        PBSHighway = 1 << 8,
        PBSTerraformProtected = 1 << 9,
        NpcRestricted = 1 << 10,

        HighWayCombo = PBSHighway | Highway,
        TerraformProtectedCombo = TerraformProtected | PBSTerraformProtected,
    }
}