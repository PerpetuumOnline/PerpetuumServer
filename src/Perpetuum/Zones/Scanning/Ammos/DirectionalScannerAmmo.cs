using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.Scanning.Ammos
{
    public class DirectionalScannerAmmo : GeoScannerAmmo
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }
    }
}