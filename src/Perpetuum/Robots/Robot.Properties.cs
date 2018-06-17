using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Units;

namespace Perpetuum.Robots
{
    partial class Robot
    {
        private UnitOptionalProperty<int> _decay;
        private UnitOptionalProperty<Color> _tint;

        private ItemProperty _powerGridMax;
        private ItemProperty _powerGrid;
        private ItemProperty _cpuMax;
        private ItemProperty _cpu;
        private ItemProperty _ammoReloadTime;
        private ItemProperty _missileHitChance;
        private ItemProperty _decayChance;

        private void InitProperties()
        {
            _decay = new UnitOptionalProperty<int>(this, UnitDataType.Decay, k.decay, () => 255);
            OptionalProperties.Add(_decay);

            _tint = new UnitOptionalProperty<Color>(this,UnitDataType.Tint,k.tint,() => ED.Config.Tint);
            OptionalProperties.Add(_tint);

            _powerGridMax = new UnitProperty(this, AggregateField.powergrid_max, AggregateField.powergrid_max_modifier);
            AddProperty(_powerGridMax);

            _powerGrid = new PowerGridProperty(this);
            AddProperty(_powerGrid);

            _cpuMax = new UnitProperty(this, AggregateField.cpu_max, AggregateField.cpu_max_modifier);
            AddProperty(_cpuMax);

            _cpu = new CpuProperty(this);
            AddProperty(_cpu);

            _ammoReloadTime = new UnitProperty(this, AggregateField.ammo_reload_time, AggregateField.ammo_reload_time_modifier);
            AddProperty(_ammoReloadTime);

            _missileHitChance = new UnitProperty(this, AggregateField.missile_miss, AggregateField.missile_miss_modifier);
            AddProperty(_missileHitChance);

            _decayChance = new DecayChanceProperty(this);
            AddProperty(_decayChance);
        }

        private double PowerGridMax
        {
            get { return _powerGridMax.Value; }
        }

        public double PowerGrid
        {
            get { return _powerGrid.Value; }
        }

        private double CpuMax
        {
            get { return _cpuMax.Value; }
        }

        public double Cpu
        {
            get { return _cpu.Value; }
        }

        public TimeSpan AmmoReloadTime
        {
            get { return TimeSpan.FromMilliseconds(_ammoReloadTime.Value); }
        }

        public double MissileHitChance
        {
            get { return _missileHitChance.Value; }
        }

        public int Decay
        {
            private get { return _decay.Value; }
            set { _decay.Value = value & 255; }
        }

        public Color Tint
        {
            get { return _tint.Value; }
            set { _tint.Value = value; }
        }

        public override void UpdateRelatedProperties(AggregateField field)
        {
            foreach (var component in RobotComponents)
            {
                component.UpdateRelatedProperties(field);
            }

            base.UpdateRelatedProperties(field);
        }

        public override Dictionary<string, object> BuildPropertiesDictionary()
        {
            var result = new Dictionary<string,object>();

            foreach (var component in RobotComponents)
            {
                var d = component.BuildPropertiesDictionary();
                result.AddRange(d);
            }

            // hogy felulirja a defaultokat
            result.AddRange(base.BuildPropertiesDictionary());

            return result;
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            var modifier = base.GetPropertyModifier(field);

            foreach (var component in RobotComponents)
            {
                var m = component.GetPropertyModifier(field);
                m.Modify(ref modifier);
            }

            return modifier;
        }

        public bool CheckPowerGridForModule(Module module)
        {
            return TotalPowerGridUsage + module.PowerGridUsage <= PowerGridMax;
        }

        public bool CheckCpuForModule(Module module)
        {
            return TotalCpuUsage + module.CpuUsage <= CpuMax;
        }

        public double TotalPowerGridUsage
        {
            get { return Modules.Sum(m => m.PowerGridUsage); }
        }

        public double TotalCpuUsage
        {
            get { return Modules.Sum(m => m.CpuUsage); }
        }

        private class DecayChanceProperty : UnitProperty
        {
            public DecayChanceProperty(Unit owner) : base(owner, AggregateField.decay_chance) { }

            protected override double CalculateValue()
            {
                var v = 20 / owner.SignatureRadius * 0.01;
                return v;
            }
        }

        private class PowerGridProperty : UnitProperty
        {
            private readonly Robot _owner;

            public PowerGridProperty(Robot owner)
                : base(owner, AggregateField.powergrid_current)
            {
                _owner = owner;
            }

            protected override double CalculateValue()
            {
                return _owner.PowerGridMax - _owner.TotalPowerGridUsage;
            }
        }

        private class CpuProperty : UnitProperty
        {
            private readonly Robot _owner;

            public CpuProperty(Robot owner)
                : base(owner, AggregateField.cpu_current)
            {
                _owner = owner;
            }

            protected override double CalculateValue()
            {
                return _owner.CpuMax - _owner.TotalCpuUsage;
            }
        }
    }
}
