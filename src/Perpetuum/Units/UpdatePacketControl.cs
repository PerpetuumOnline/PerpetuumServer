namespace Perpetuum.Units
{
    /// <summary>
    /// Controls the client's behaviour on receiving an update packet on the zone
    /// </summary>
    public enum UpdatePacketControl : byte
    {
        Undefined,
        ForceReposition,
    }
}