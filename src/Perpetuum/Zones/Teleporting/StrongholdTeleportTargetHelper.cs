using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Teleporting
{
    public class StrongholdTeleportTargetHelper
    {
        private readonly IZoneManager _zoneManager;
        private readonly TeleportDescriptionBuilder.Factory _descriptionBuilderFactory;

        public StrongholdTeleportTargetHelper(IZoneManager zoneManager, TeleportDescriptionBuilder.Factory descriptionBuilderFactory)
        {
            _zoneManager = zoneManager;
            _descriptionBuilderFactory = descriptionBuilderFactory;
        }

        public IEnumerable<TeleportDescription> GetStrongholdTargets(IZone zone, long sourceTeleportEid, int teleportRange)
        {
            var result = new List<TeleportDescription>();
            var descriptionId = 0;
            foreach (var teleportColumn in _zoneManager.Zones.OfType<StrongHoldZone>().GetUnits<TeleportColumn>().Where(tc => tc.IsEnabled && tc.Zone != zone))
            {

                var builder = _descriptionBuilderFactory();
                builder.SetId(descriptionId++)
                    .SetType(TeleportDescriptionType.AnotherZone)
                    .SetSourceTeleport(sourceTeleportEid)
                    .SetSourceZone(zone)
                    .SetSourceRange(teleportRange)
                    .SetTargetZone(teleportColumn.Zone)
                    .SetTargetTeleport(teleportColumn)
                    .SetTargetRange(7)
                    .SetListable(true)
                    .SetActive(teleportColumn.IsEnabled);

                var td = builder.Build();
                result.Add(td);
            }
            return result;
        }
    }
}