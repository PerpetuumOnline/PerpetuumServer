using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;

namespace Perpetuum.Modules
{
    public abstract class EnergyDispersionModule : ActiveModule
    {
        private readonly ItemProperty _energyDispersion;

        protected EnergyDispersionModule() : base(true)
        {
            _energyDispersion = new ModuleProperty(this,AggregateField.energy_dispersion);
            AddProperty(_energyDispersion);
        }

        protected void ModifyValueByReactorRadiation(Unit enemy,ref double value)
        {
            var modifier = enemy.ReactorRadiation / _energyDispersion.Value;

            if (modifier <= 0.0 || modifier >= 1.0)
            {
                modifier = 1.0;
            }

            value *= modifier;
        }
    }

    public class EnergyDispersionEventArgs : CombatEventArgs
    {
        public double Amount { get; private set; }

        public EnergyDispersionEventArgs(double amount)
        {
            Amount = amount;
        }
    }

}