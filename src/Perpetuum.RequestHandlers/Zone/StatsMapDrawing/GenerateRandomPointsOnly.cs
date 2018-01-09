using System.Drawing;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.StatsMapDrawing
{
    public partial class ZoneDrawStatMap
    {

        private Bitmap GenerateRandomPointsOnly(IRequest request)
        {
            //-------- kick brute force fill in
            const int randomPointTargetAmount = 2500;

            //----code

            var deletedSpots =
            Db.Query().CommandText("delete missionspotinfo where zoneid=@zoneId and type=@spotType")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@spotType", (int) MissionSpotType.randompoint )
                .ExecuteNonQuery();

            Logger.Info(deletedSpots + " random point spots were deleted from zone:" + _zone.Id);

            var staticObjects = MissionSpot.GetStaticObjectsFromZone(_zone);

            var spotInfos = MissionSpot.GetMissionSpotsFromUnitsOnZone(_zone);

            PlaceOneType(spotInfos, MissionSpotType.randompoint, randomPointTargetAmount, randomPointDistanceInfos, staticObjects, randomPointAccuracy);

            var resultBitmap = DrawResultOnBitmap(spotInfos,staticObjects);

            SendDrawFunctionFinished(request);

            var randomPoints = spotInfos.Where(i => i.type == MissionSpotType.randompoint).ToList();

            Logger.Info(randomPoints.Count + " new random points were generated.");

            //-------

            
            var deletedTargets=
            Db.Query().CommandText("delete missiontargets where targetpositionzone=@zoneId and targettype=@targetType")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@targetType", (int)MissionTargetType.rnd_point)
                .ExecuteNonQuery();

            Logger.Info(deletedTargets + " mission targets were deleted.");


            foreach (var missionSpot in randomPoints)
            {
                MissionHelper.PlaceRandomPoint(_zone, missionSpot.position, (int)DistanceConstants.MISSION_RANDOM_POINT_FINDRADIUS_DEFAULT);
            }
            
            Logger.Info("new mission targets inserted");

            return resultBitmap;
        }


    }
}
