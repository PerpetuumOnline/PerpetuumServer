using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZonePutPlant : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var x = request.Data.GetOrDefault<int>(k.x);
            var y = request.Data.GetOrDefault<int>(k.y);
            var plantIndex = request.Data.GetOrDefault<int>(k.index);
            var plantState = request.Data.GetOrDefault<int>(k.state);

            if (!new Position(x, y).IsValid(request.Zone.Size))
            {
                throw new PerpetuumException(ErrorCodes.InvalidPosition);
            }

            var rule = request.Zone.Configuration.PlantRules.FirstOrDefault(r => r.Type == (PlantType)plantIndex);
            if (rule == null)
                throw new PerpetuumException(ErrorCodes.PlantNotFertileOnThisZone);

            using (new TerrainUpdateMonitor(request.Zone))
            {
                request.Zone.Terrain.PutPlant(x, y, (byte)plantState, (PlantType)plantIndex, rule);
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}