using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionTargets;

namespace Perpetuum.RequestHandlers.Zone.MissionRequests
{
    public class MissionSpotUpdate : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var targetId = request.Data.GetOrDefault<int>(k.ID);

                var x = request.Data.GetOrDefault<int>(k.x);
                var y = request.Data.GetOrDefault<int>(k.y);

                MissionTarget.UpdatePosition(targetId, x, y);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}
