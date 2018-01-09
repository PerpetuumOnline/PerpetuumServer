using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSetQueueLength : ZoneGetQueueInfo
    {
        public override void HandleRequest(IZoneRequest request)
        {
            request.Zone.EnterQueueService.MaxPlayersOnZone = request.Data.GetOrDefault(k.length, request.Zone.Configuration.MaxPlayers);
            base.HandleRequest(request);
        }
    }
}