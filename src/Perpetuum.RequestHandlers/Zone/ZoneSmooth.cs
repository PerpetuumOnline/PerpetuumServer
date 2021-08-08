using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;
using System.Threading.Tasks;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSmooth : IRequestHandler<IZoneRequest>
    {
        private int CalculateBufferOffset(int x, int y, Area area)
        {
            return (x - area.X1) + (y - area.Y1) * area.Width;
        }
        public void HandleRequest(IZoneRequest request)
        {
            var zone = request.Zone;
            zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var area = Area.FromRectangle(0, 0, zone.Size.Width, zone.Size.Height);
            var altBuffer = new ushort[area.Ground];
            var workAreas = area.Slice(32);
            Parallel.ForEach(workAreas, (workArea) =>
            {
                foreach (var p in workArea.GetPositions())
                {
                    var sum = 0.0;
                    var count = 0;
                    foreach (var n in p.EightNeighbours)
                    {
                        if (zone.IsValidPosition(n.intX, n.intY))
                        {
                            sum += zone.Terrain.Altitude.GetAltitudeAsDouble(n.intX, n.intY);
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        var smoothed = sum / count;
                        var shortAlt = System.Convert.ToUInt16(smoothed * 32);
                        altBuffer[CalculateBufferOffset(p.intX, p.intY, area)] = shortAlt;
                    }
                }
            });
            Parallel.ForEach(workAreas, (workArea) =>
            {
                foreach (var p in workArea.GetPositions())
                {
                    zone.Terrain.Altitude.SetValue(p.intX, p.intY, altBuffer[CalculateBufferOffset(p.intX, p.intY, area)]);
                }
                zone.Terrain.Slope.UpdateSlopeByArea(workArea);
            });
        }
    }
}
