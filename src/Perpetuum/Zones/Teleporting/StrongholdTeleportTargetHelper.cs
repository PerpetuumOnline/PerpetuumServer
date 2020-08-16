using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Teleporting
{
    public class StrongholdTeleportTargetHelper
    {
        private readonly IZoneManager _zoneManager;
        private readonly TeleportDescriptionBuilder.Factory _descriptionBuilderFactory;
        private readonly IMobileTeleportToZoneMap _mobileTeleportToZoneMap;

        public StrongholdTeleportTargetHelper(IZoneManager zoneManager, TeleportDescriptionBuilder.Factory descriptionBuilderFactory, IMobileTeleportToZoneMap mobileTeleportToZoneMap)
        {
            _zoneManager = zoneManager;
            _descriptionBuilderFactory = descriptionBuilderFactory;
            _mobileTeleportToZoneMap = mobileTeleportToZoneMap;
        }

        public IEnumerable<TeleportDescription> GetStrongholdTargets(IZone zone, long sourceTeleportEid, int teleportRange, int mobileDefinition)
        {
            var result = new List<TeleportDescription>();
            var descriptionId = 0;
            var targetZoneIds = _mobileTeleportToZoneMap.GetDestinationZones(mobileDefinition);
            var zones = _zoneManager.Zones.OfType<StrongHoldZone>().Where(z => targetZoneIds.Contains(z.Id));
            var teleportCols = zones.GetUnits<TeleportColumn>().Where(tc => tc.IsEnabled && tc.Zone != zone);
            foreach (var teleportColumn in teleportCols)
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