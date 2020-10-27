using Perpetuum.Units;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    /// <summary>
    /// Handles application of any/all ZoneEffects
    /// </summary>
    public interface IZoneEffectHandler
    {
        /// <summary>
        /// Initializes all ZoneEffects on a unit entering the zone
        /// </summary>
        /// <param name="unit">Unit entering zone</param>
        void OnEnterZone(Unit unit);

        /// <summary>
        /// Method to remove active ZoneEffect on the zone
        /// </summary>
        /// <param name="zoneEffect">ZoneEffect to remove</param>
        void RemoveEffect(ZoneEffect zoneEffect);

        /// <summary>
        /// Method to add ZoneEffect to the zone
        /// </summary>
        /// <param name="zoneEffect">ZoneEffect to add</param>
        void AddEffect(ZoneEffect zoneEffect);
    }
}
