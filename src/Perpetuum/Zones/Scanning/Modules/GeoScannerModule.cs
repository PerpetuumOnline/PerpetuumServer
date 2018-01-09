using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Zones.Scanning.Results;
using Perpetuum.Zones.Scanning.Scanners;

namespace Perpetuum.Zones.Scanning.Modules
{
    public class GeoScannerModule : ActiveModule
    {
        private readonly Scanner.Factory _scannerFactory;
        private readonly ItemProperty _miningProbeAccuracy;

        public MineralScanResult LastScanResult { get; set; }

        public GeoScannerModule(CategoryFlags ammoCategoryFlags,Scanner.Factory scannerFactory) : base(ammoCategoryFlags)
        {
            _scannerFactory = scannerFactory;
            _miningProbeAccuracy = new MiningProbeAccuracy(this);
            AddProperty(_miningProbeAccuracy);
        }

        public double ScanAccuracy => _miningProbeAccuracy.Value;

        private class MiningProbeAccuracy : ModuleProperty
        {
            public MiningProbeAccuracy(GeoScannerModule module) : base(module, AggregateField.mining_probe_accuracy)
            {
                AddEffectModifier(AggregateField.effect_mining_probe_accuracy_modifier);
            }

            protected override double CalculateValue()
            {
                var result = base.CalculateValue();
                result = result.Clamp();
                return result;
            }
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.mining_probe_accuracy:
                case AggregateField.mining_probe_accuracy_modifier:
                case AggregateField.effect_mining_probe_accuracy_modifier:
                    {
                        _miningProbeAccuracy.Update();
                        break;
                    }
            }

            base.UpdateProperty(field);
        }

        protected override void OnAction()
        {
            var player = (Player)ParentRobot;

            var zone = player.Zone;
            if (zone == null) 
                return;

            var ammo = GetAmmo();
            if (ammo == null)
                return;

            var scanner = _scannerFactory(zone, player, this);
            ammo.AcceptVisitor(scanner);
            ConsumeAmmo();
        }
    }
}