using Perpetuum.Players;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    public class ZoneEffectHandler : IZoneEffectHandler
    {
        private readonly IZoneEffectRepository _zoneEffectRepository;
        public ZoneEffectHandler(IZoneEffectRepository zoneEffectRepository)
        {
            _zoneEffectRepository = zoneEffectRepository;
        }

        public void ApplyZoneEffects(Player player, IZone zone)
        {
            var zoneEffects = _zoneEffectRepository.GetZoneEffects(zone);
            foreach (var zoneEffect in zoneEffects)
            {
                var builder = player.NewEffectBuilder().SetType(zoneEffect.Effect).SetOwnerToSource();
                player.ApplyEffect(builder);
            }
        }
    }
}
