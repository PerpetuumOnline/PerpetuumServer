using System.Drawing;
using Perpetuum.Services.MissionEngine;

namespace Perpetuum.RequestHandlers.Zone.StatsMapDrawing
{
    public partial class ZoneDrawStatMap
    {
        private Bitmap DisplaySpots()
        {

            var staticObjects = MissionSpot.GetStaticObjectsFromZone(_zone);

            var spotInfos = MissionSpot.GetMissionSpotsFromUnitsOnZone(_zone);

            var randomPointsInfos = MissionSpot.GetRandomPointSpotsFromTargets(_zone.Configuration);

            spotInfos.AddRange(randomPointsInfos);

            return  DrawResultOnBitmap(spotInfos, staticObjects);

        }
    }
}
