using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZonePBSTest : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            //clean pbshighway bit
            request.Zone.Terrain.Controls.UpdateAll((x, y, ci) =>
            {
                ci.PBSHighway = false;
                return ci;
            });
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}