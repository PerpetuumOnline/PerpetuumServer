using Perpetuum.ExportedTypes;

using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.Scanning.Ammos
{
    public abstract class GeoScannerAmmo : Ammo
    {
        private const int DEFAULT_MINING_PROBE_RANGE = 30;
        private readonly ItemProperty _miningProbeRange;

        protected GeoScannerAmmo(AggregateField miningProbeRange = AggregateField.undefined,AggregateField miningProbeRangeModifier = AggregateField.undefined)
        {
            _miningProbeRange = new MiningProbeRangeProperty(this,miningProbeRange,miningProbeRangeModifier);
            AddProperty(_miningProbeRange);
        }

        public MaterialType MaterialType => ED.Options.MineralLayer.ToMaterialType();

        public int ScanRange
        {
            get { return (int) _miningProbeRange.Value; }
        }

        private class MiningProbeRangeProperty : AmmoProperty<GeoScannerAmmo>
        {
            private readonly AggregateField _miningProbeRange;
            private readonly AggregateField _miningProbeRangeModifier;

            public MiningProbeRangeProperty(GeoScannerAmmo ammo,AggregateField miningProbeRange,AggregateField miningProbeRangeModifier) : base(ammo, AggregateField.mining_probe_range)
            {
                _miningProbeRange = miningProbeRange;
                _miningProbeRangeModifier = miningProbeRangeModifier;
            }

            protected override double CalculateValue()
            {
                double range = DEFAULT_MINING_PROBE_RANGE;

                if ( _miningProbeRange != AggregateField.undefined )
                {
                    var m = ammo.GetPropertyModifier(_miningProbeRange);
                    if (m.HasValue)
                    {
                        range = m.Value;
                    }
                }

                if (_miningProbeRangeModifier == AggregateField.undefined) 
                    return range;

                var robot = ammo.GetParentRobot();
                if (robot == null) 
                    return range;

                var rangeModifier = robot.GetPropertyModifier(_miningProbeRangeModifier);
                rangeModifier.Modify(ref range);

                return range;
            }
        }
    }
}