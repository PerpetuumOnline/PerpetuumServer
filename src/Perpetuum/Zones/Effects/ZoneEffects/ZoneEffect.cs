using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    /// <summary>
    /// An effect to be applied to everyone on a give zone
    /// </summary>
    public class ZoneEffect
    {
        public int ZoneId { get; private set; }
        public EffectType Effect { get; private set; }
        public bool PlayerOnly { get; private set; }

        public ZoneEffect(int zoneId, EffectType effectType, bool forPlayersOnly)
        {
            ZoneId = zoneId;
            Effect = effectType;
            PlayerOnly = forPlayersOnly;
        }

        public override bool Equals(object obj)
        {
            if (obj is ZoneEffect z)
            {
                return Equals(z);
            }
            return base.Equals(obj);
        }

        public bool Equals(ZoneEffect other)
        {
            return other != null &&
                ZoneId == other.ZoneId &&
                Effect == other.Effect &&
                PlayerOnly == other.PlayerOnly;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ZoneId * 31;
                hashCode *= 31 + Effect.GetHashCode();
                hashCode *= 31 + PlayerOnly.GetHashCode();
                return hashCode;
            }
        }
    }
}
