namespace Perpetuum.Zones.Beams
{
    /// <summary>
    /// This state controls the display of a beam on client side
    /// </summary>
    public enum BeamState
    {
        Hit, //hit the target
        Miss, //misses the target
        AlignToTerrain, //positioned onto the terrain surface
        WreckSelect //display a unit wreck beam
    }
}