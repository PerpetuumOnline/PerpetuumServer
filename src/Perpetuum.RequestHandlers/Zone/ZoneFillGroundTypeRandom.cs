using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;
using System;
using System.Linq;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneFillGroundTypeRandom : IRequestHandler<IZoneRequest>
    {
        private bool IsGroundTypeFilled(ILayer<PlantInfo> plants)
        {
            return !plants.RawData.Any(p => p.groundType == GroundType.undefined);
        }

        private void SetGroundTypeWithCircle(ILayer<PlantInfo> plants, Position center, int radius, GroundType groundType)
        {
            var x0 = (center.intX - radius).Clamp(0, plants.Width - 1);
            var x1 = (center.intX + radius).Clamp(0, plants.Width);
            var y0 = (center.intY - radius).Clamp(0, plants.Width - 1);
            var y1 = (center.intY + radius).Clamp(0, plants.Width);
            var period = FastRandom.NextInt(3, 32);
            var fuzzPercent = FastRandom.NextDouble(0.05, 0.5);
            var noiseMagnitude = FastRandom.NextInt(1, (int)(radius * 0.25));
            for (int x = x0; x < x1; x++)
            {
                for (int y = y0; y < y1; y++)
                {
                    var p = new Position(x, y);
                    var angle = center.DirectionTo(p) * Math.PI;
                    var sin = (Math.Sin(angle * period) + 1.0) * 0.5;
                    var noise = (FastRandom.NextDouble()-0.5)* noiseMagnitude;
                    var boundary = radius * (1.0 - fuzzPercent) + (radius * sin * fuzzPercent);
                    if (center.TotalDistance2D(x, y) > (boundary + noise).Clamp(1, radius)) continue;
                    var info = plants.GetValue(x, y);
                    info.SetGroundType(groundType);
                    plants[x, y] = info;
                }
            }
        }

        private GroundType PickRandom()
        {
            var values = ((GroundType[])Enum.GetValues(typeof(GroundType))).Where(g => g != GroundType.undefined).ToArray();
            return values[FastRandom.NextInt(values.Length - 1)];
        }

        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var minRadius = request.Data.GetOrDefault<int>(k.size).Clamp(2, 200);
            var iterations = request.Data.GetOrDefault<int>(k.numberOfRuns).Clamp(1, 1000);
            var zoneRect = request.Zone.Size;
            var plants = request.Zone.Terrain.Plants;
            Logger.Info($"{request.Zone.Id} filling groundtype with {iterations} and brush radius {minRadius}");
            for (var i = 0; i < iterations; i++)
            {
                if (i % 100 == 0)
                {
                    Logger.Info($"{request.Zone.Id} filling groundtype iteration {i}/{iterations}...");
                }
                var pos = zoneRect.GetRandomPosition(1);
                var radius = (int)(FastRandom.NextDouble() * (minRadius * 0.5) + minRadius);
                SetGroundTypeWithCircle(plants, pos, radius, PickRandom());
            }
            Logger.Info($"{request.Zone.Id} groundtype filled? {IsGroundTypeFilled(plants)}");
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}