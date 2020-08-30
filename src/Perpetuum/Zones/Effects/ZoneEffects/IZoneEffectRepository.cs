using System.Collections.Generic;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    /// <summary>
    /// Interface to lookup ZoneEffects by zone
    /// </summary>
    public interface IZoneEffectRepository
    {
        List<ZoneEffect> GetZoneEffects(IZone zone);
    }
}
