using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine;

namespace Perpetuum.RequestHandlers.Zone.MissionRequests
{
    public class ZoneUpdateStructure :IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var orientation = request.Data.GetOrDefault(k.orientation, -1);
                var x = request.Data.GetOrDefault<double>(k.x);
                var y = request.Data.GetOrDefault<double>(k.y);

                MissionHelper.UpdateMissionStructure(request.Zone,eid,orientation,new Position(x,y));
                Message.Builder.FromRequest(request).WithOk().Send();
               
                scope.Complete();
            }           
        }
    }
}
