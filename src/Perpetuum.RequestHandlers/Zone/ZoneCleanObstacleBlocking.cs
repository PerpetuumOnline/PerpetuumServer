using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCleanObstacleBlocking : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.Terrain.Blocks.UpdateAll((x, y, bi) =>
            {
                bi.Obstacle = false;
                return bi;
            });

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}