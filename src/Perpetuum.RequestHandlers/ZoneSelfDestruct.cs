using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneSelfDestruct : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public ZoneSelfDestruct(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var player = _zoneManager.GetPlayer(request.Session.Character);
            player?.Kill();
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}