using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Scanning.Ammos
{
    public class ArtifactScannerAmmo : GeoScannerAmmo
    {
        public ArtifactScannerAmmo() : base(AggregateField.mining_probe_artifact_range,AggregateField.mining_probe_artifact_range_modifier)
        {

        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }
    }
}