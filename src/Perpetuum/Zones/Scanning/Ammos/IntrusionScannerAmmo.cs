using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Zones.Scanning.Ammos
{
    public class IntrusionScannerAmmo : GeoScannerAmmo
    {
        public IntrusionScannerAmmo() : base(AggregateField.mining_probe_intrusion_range, AggregateField.mining_probe_intrusion_range_modifier)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void ModifyCycleTime(ref ItemPropertyModifier modifier)
        {
            var m = GetPropertyModifier(AggregateField.mining_probe_cycle_time_intrusion_modifier);
            m.Modify(ref modifier);
        }
    }
}