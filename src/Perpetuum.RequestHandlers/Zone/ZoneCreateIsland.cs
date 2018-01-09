using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCreateIsland : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var low = request.Data.GetOrDefault<int>(k.low);
            var waterLevel = ZoneConfiguration.WaterLevel;

            low = low.Clamp(0, waterLevel - 1);

            request.Zone.Terrain.Blocks.UpdateAll((x, y, bi) =>
            {
                var altitudeVal = request.Zone.Terrain.Altitude.GetAltitude(x, y);
                var isBelow = (altitudeVal < waterLevel - low);
                bi.Island = isBelow;
                return bi;
            });

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}