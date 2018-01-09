using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDrawBlockingByEid : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public ZoneDrawBlockingByEid(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var eid = request.Data.GetOrDefault<long>(k.eid);
            var unit = _zoneManager.GetUnit<Unit>(eid);
            if (unit == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);

            unit.Zone.DrawEnvironmentByUnit(unit);

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}