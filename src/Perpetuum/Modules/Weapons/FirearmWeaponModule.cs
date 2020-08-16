using Perpetuum.ExportedTypes;
using Perpetuum.Units;

namespace Perpetuum.Modules.Weapons
{
    /// <summary>
    /// Special subclass of weapon with special capabilities against plants
    /// applies damage like missiles and doesn't miss.
    /// </summary>
    public class FirearmWeaponModule : WeaponModule
    {
        public ModuleProperty PlantDamageModifier { get; }

        public FirearmWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            PlantDamageModifier = new ModuleProperty(this, AggregateField.damage_toxic_modifier);
            AddProperty(PlantDamageModifier);
        }

        protected override bool CheckAccuracy(Unit victim)
        {
            return false;
        }

        protected override IDamageBuilder GetDamageBuilder()
        {
            return base.GetDamageBuilder()
                .WithExplosionRadius(Accuracy.Value);
        }
    }
}
