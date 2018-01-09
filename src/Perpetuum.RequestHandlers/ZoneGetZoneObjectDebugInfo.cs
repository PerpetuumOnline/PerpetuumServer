using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneGetZoneObjectDebugInfo : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public ZoneGetZoneObjectDebugInfo(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var targetEid = request.Data.GetOrDefault<long>(k.targetEID);
            var unit = _zoneManager.GetUnit<Unit>(targetEid);
            if (unit == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);

            var info = new Dictionary<string, object>
            {
                {"targetEID", unit.Eid},
                {k.data, unit.GetDebugInfo()}
            };

            Message.Builder.FromRequest(request).WithData(info).Send();
        }
    }
}