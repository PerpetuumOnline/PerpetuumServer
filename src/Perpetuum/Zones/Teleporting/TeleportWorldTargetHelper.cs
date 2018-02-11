using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Teleporting
{
    public class TeleportWorldTargetHelper
    {
        private readonly IZoneManager _zoneManager;
        private readonly TeleportDescriptionBuilder.Factory _descriptionBuilderFactory;

        public TeleportWorldTargetHelper(IZoneManager zoneManager,TeleportDescriptionBuilder.Factory descriptionBuilderFactory)
        {
            _zoneManager = zoneManager;
            _descriptionBuilderFactory = descriptionBuilderFactory;
        }

        public IEnumerable<TeleportDescription> GetWorldTargets(IZone zone,Position position,long sourceTeleporEid,int teleporRange,int collectColumnsDistance)
        {
            var result = new List<TeleportDescription>();

            //generate a teleport description for all the columns in the world
            var descriptionId = 0;
            var myWorldPos = zone.ToWorldPosition(position);

            foreach (var teleportColumn in _zoneManager.Zones.GetUnits<TeleportColumn>().Where(tc => tc.IsEnabled && tc.Zone != zone))
            {
                var targetWorldPos = teleportColumn.Zone.ToWorldPosition(teleportColumn.CurrentPosition);

                if (!myWorldPos.IsInRangeOf2D(targetWorldPos,collectColumnsDistance))
                    continue;

                var builder = _descriptionBuilderFactory();
                builder.SetId(descriptionId++)
                    .SetType(TeleportDescriptionType.AnotherZone)
                    .SetSourceTeleport(sourceTeleporEid)
                    .SetSourceZone(zone)
                    .SetSourceRange(teleporRange)
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