using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneHealAllWalls : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var wallRule = request.Zone.Configuration.PlantRules.GetPlantRule(PlantType.Wall);

            request.Zone.Terrain.Plants.UpdateAll((x, y, pi) =>
            {
                if (pi.type != PlantType.Wall)
                    return pi;

                pi.health = wallRule.Health[pi.state];
                return pi;
            });
        }
    }
}