using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDrawBeam : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var x = request.Data.GetOrDefault<double>(k.x);
            var y = request.Data.GetOrDefault<double>(k.y);
            var inBeamType = request.Data.GetOrDefault<int>(k.beam);

            x = x.Clamp(0, 2047);
            y = y.Clamp(0, 2047);

            var outBeam = BeamType.red_20sec;
            if (inBeamType != 0)
            {
                outBeam = (BeamType) inBeamType;
            }

            request.Zone.CreateBeam(outBeam,builder => builder.WithPosition(new Position(x,y)).WithDuration(404040));
        }
    }
}
