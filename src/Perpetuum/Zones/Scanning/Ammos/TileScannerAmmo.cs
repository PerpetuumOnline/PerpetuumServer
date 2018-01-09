using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Scanning.Ammos
{
    public class TileScannerAmmo : GeoScannerAmmo
    {
        public TileScannerAmmo() : base(AggregateField.tile_based_mining_probe_radius, AggregateField.mining_probe_tile_range_modifier)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }
    }
}