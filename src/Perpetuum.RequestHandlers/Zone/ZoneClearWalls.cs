using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneClearWalls : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.Terrain.Plants.UpdateAll((x, y, pi) =>
            {
                if (pi.type == PlantType.Wall)
                    pi.Clear();

                return pi;
            });

            request.Zone.Terrain.Blocks.UpdateAll((x, y, bi) =>
            {
                bi.Plant = false;
                return bi;
            });

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}