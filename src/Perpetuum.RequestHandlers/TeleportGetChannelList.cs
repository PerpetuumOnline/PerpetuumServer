using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.RequestHandlers
{
    public class TeleportGetChannelList : IRequestHandler<IZoneRequest>
    {
        private readonly IZoneManager _zoneManager;

        public TeleportGetChannelList(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var teleportEid = request.Data.GetOrDefault<long>(k.eid);
            var teleport = _zoneManager.GetUnit<Teleport>(teleportEid);
            if (teleport == null)
                throw new PerpetuumException(ErrorCodes.TeleportNotFound);

            var result = teleport.ToDictionary();
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}