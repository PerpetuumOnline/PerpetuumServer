using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCheckRoaming : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.ForEachAll((x, y) => CheckRoamingConditions(request.Zone,x,y));
            Message.Builder.FromRequest(request).WithOk().Send();
        }

        private void CheckRoamingConditions(IZone zone,int x, int y)
        {
            var controlInfo = zone.Terrain.Controls.GetValue(x, y);
            if (!controlInfo.Roaming) 
                return;

            if (zone.Terrain.IsBlocked(x,y))
            {
                Logger.Error($"consistency error: roaming blocked. zone:{zone.Id} x:{x} y:{y}");
                return;
            }

            if (!zone.Terrain.Slope.CheckSlope(x, y, 4))
            {
                Logger.Error($"consistency error: roaming on bad slope. zone:{zone.Id} x:{x} y:{y}");
            }
        }
    }
}
