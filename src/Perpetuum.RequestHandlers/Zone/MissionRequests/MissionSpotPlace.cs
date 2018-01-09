using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.MissionRequests
{

    /// <summary>
    /// Inserts a missiontarget which can be used as possible random mission target's position
    /// </summary>
    public class MissionSpotPlace : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var position = request.Zone.GetPrimaryLockedTileOrThrow(character);
                var findRadius = request.Data.GetOrDefault(k.radius, (int)DistanceConstants.MISSION_RANDOM_POINT_FINDRADIUS_DEFAULT);
                MissionHelper.PlaceRandomPoint(request.Zone,position,findRadius);
                Message.Builder.FromRequest(request).WithOk().Send();
                scope.Complete();
            }
        }
    }

}
