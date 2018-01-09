using Perpetuum.EntityFramework;
using Perpetuum.Zones.Scanning.Ammos;
using Perpetuum.Zones.Scanning.Results;
using Perpetuum.Zones.Terrains.Materials.Minerals;

namespace Perpetuum.Zones.Scanning.Scanners
{
    public partial class Scanner : IEntityVisitor<TileScannerAmmo>
    {
        public void Visit(TileScannerAmmo ammo)
        {
            var area = _zone.CreateArea(_player.CurrentPosition, ammo.ScanRange);

            var builder = MineralScanResultBuilder.Create(_player.Zone, ammo.MaterialType);
            builder.ScanArea = area;
            builder.ScanAccuracy = _module.ScanAccuracy;

            var result = builder.Build();

            _module.LastScanResult = result;

            var mineralLayer = _zone.Terrain.GetMaterialLayer(ammo.MaterialType) as MineralLayer;
            if (mineralLayer != null)
            {
                if (result.FoundAny)
                {
                    OnMineralScanned(MaterialProbeType.Tile, ammo.MaterialType);
                }
            }

            _player.Session.SendPacket(result.ToPacket());
        }
    }
}
