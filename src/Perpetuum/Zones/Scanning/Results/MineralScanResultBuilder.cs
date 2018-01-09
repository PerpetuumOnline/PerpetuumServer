using System;
using Perpetuum.Builders;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;

namespace Perpetuum.Zones.Scanning.Results
{
    public class MineralScanResultBuilder : IBuilder<MineralScanResult>
    {
        private readonly MineralLayer _mineralLayer;

        public Area ScanArea { private get; set; }
        public double ScanAccuracy { private get; set; }

        private MineralScanResultBuilder(MineralLayer mineralLayer)
        {
            _mineralLayer = mineralLayer;
        }

        public static MineralScanResultBuilder Create(IZone zone, MaterialType materialType)
        {
            var mineralLayer = zone.Terrain.GetMineralLayerOrThrow(materialType);
            return new MineralScanResultBuilder(mineralLayer);
        }
       
        public MineralScanResult Build()
        {
            var nodes = _mineralLayer.GetNodesByArea(ScanArea);
            var foundAny = false;

            var data = new uint[ScanArea.Width * ScanArea.Height];

            long sum = 0;

            var offset = 0;
            for (var y = ScanArea.Y1; y <= ScanArea.Y2; y++)
            {
                for (var x = ScanArea.X1; x <= ScanArea.X2; x++)
                {
                    if (ScanArea.ContainsInInnerCircle(x, y))
                    {
                        foreach (var node in nodes)
                        {
                            var value = node.GetValue(x, y);
                            if (value <= 0)
                                continue;

                            foundAny = true;

                            var m = FastRandom.NextDouble(ScanAccuracy, 1.0);
                            uint a = (uint) (value * m);
                            data[offset] = a;

                            sum += a;
                            break;
                        }
                    }

                    offset++;
                }
            }

            var result = new MineralScanResult(data)
            {
                ScanAccuracy = ScanAccuracy,
                FoundAny = foundAny,
                Area = ScanArea,
                MaterialType = _mineralLayer.Type,
                ZoneId = _mineralLayer.Configuration.ZoneId,
                Creation = DateTime.Now,
                Quality = sum
            };

            return result;
        }
    }
}