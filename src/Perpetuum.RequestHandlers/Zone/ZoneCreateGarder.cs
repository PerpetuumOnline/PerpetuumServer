using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCreateGarder : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var x = request.Data.GetOrDefault<int>(k.x);
            var y = request.Data.GetOrDefault<int>(k.y);

            LayerHelper.CreateGarden(request.Zone, x, y);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}