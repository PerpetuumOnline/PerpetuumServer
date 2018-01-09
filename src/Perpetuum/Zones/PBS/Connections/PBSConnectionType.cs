namespace Perpetuum.Zones.PBS.Connections
{
    /// <summary>
    /// Additional information for a connection between two pbs nodes
    /// </summary>
    public enum PBSConnectionType
    {
        undefined,
        generic,
        energy,
        production,
        effect,
        armorRepair,
        control,
        highway
    }
}