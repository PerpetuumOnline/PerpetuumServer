using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneRestoreOriginalGamma : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            if (!request.Zone.Configuration.Terraformable)
                return;

            var altitudeLayer = (TerraformableAltitude)request.Zone.Terrain.Altitude;

            for (int i = 0; i < altitudeLayer.RawData.Length; i++)
            {
                var o = altitudeLayer.OriginalAltitude.RawData[i];
                altitudeLayer.RawData[i] = o;
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}