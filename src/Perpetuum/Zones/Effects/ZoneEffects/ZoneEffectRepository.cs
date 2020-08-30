using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    public class ZoneEffectRepository : IZoneEffectRepository
    {
        private readonly ConcurrentDictionary<int, List<ZoneEffect>> cache;
        public ZoneEffectRepository()
        {
            cache = new ConcurrentDictionary<int, List<ZoneEffect>>();
        }

        private List<ZoneEffect> GetZoneEffectIfNotCached(IZone zone)
        {
            var zoneEffectReader = new ZoneEffectReader(zone);
            return zoneEffectReader.GetZoneEffects().ToList();
        }

        public List<ZoneEffect> GetZoneEffects(IZone zone)
        {
            return cache.GetOrAdd(zone.Id, () => GetZoneEffectIfNotCached(zone));
        }
    }
}
