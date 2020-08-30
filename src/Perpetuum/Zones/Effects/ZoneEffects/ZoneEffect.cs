using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    /// <summary>
    /// An effect to be applied to everyone on a give zone
    /// </summary>
    public class ZoneEffect
    {
        public IZone Zone { get; private set; }
        public EffectType Effect { get; private set; }

        public ZoneEffect(IZone zone, EffectType effectType)
        {
            Zone = zone;
            Effect = effectType;
        }
    }
}
