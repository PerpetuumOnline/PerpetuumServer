using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneRemoveObject : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public ZoneRemoveObject(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var targetEid = request.Data.GetOrDefault(k.target, 0L);
            var unit = _zoneManager.GetUnit<Unit>(targetEid);
            unit?.RemoveFromZone();
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}