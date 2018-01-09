using Perpetuum.ExportedTypes;

namespace Perpetuum.Items.Ammos
{
    public class AmmoProperty<T> : ItemProperty where T : Ammo
    {
        protected readonly T ammo;

        public AmmoProperty(T ammo, AggregateField field)
            : base(field)
        {
            this.ammo = ammo;
        }

        protected override double CalculateValue()
        {
            var mod = ammo.GetPropertyModifier(Field);
            return mod.Value;
        }
    }
}