using Perpetuum.Players;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    /// <summary>
    /// Handles application of any/all ZoneEffects
    /// </summary>
    public interface IZoneEffectHandler
    {
        void ApplyZoneEffects(Player player, IZone zone);
    }
}
