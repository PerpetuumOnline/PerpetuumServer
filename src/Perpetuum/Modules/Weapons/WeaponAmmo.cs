using System.Collections.Generic;
using System.Threading;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;

namespace Perpetuum.Modules.Weapons
{
    public class WeaponAmmo : Ammo
    {
        private ItemProperty _optimalRangeModifier = ItemProperty.None;

        public override void Initialize()
        {
            if (!IsCategory(CategoryFlags.cf_missile_ammo))
            {
                _optimalRangeModifier = new AmmoProperty<WeaponAmmo>(this,AggregateField.optimal_range_modifier);
                AddProperty(_optimalRangeModifier);
            }
            base.Initialize();
        }

        public override void UpdateAllProperties()
        {
            base.UpdateAllProperties();
            UpdateCleanDamages();
        }

        private IList<Damage> _cleanDamages;

        public IList<Damage> GetCleanDamages()
        {
            return LazyInitializer.EnsureInitialized(ref _cleanDamages,CalculateCleanDamages);
        }

        private IList<Damage> CalculateCleanDamages()
        {
            var result = new List<Damage>();

            var weapon = this.GetParentModule() as WeaponModule;
            if (weapon == null)
                return result;

            var damageModifier = weapon.DamageModifier.ToPropertyModifier();

            var property = GetPropertyModifier(AggregateField.damage_chemical);

            if (property.HasValue)
            {
                damageModifier.Modify(ref property);
                result.Add(new Damage(DamageType.Chemical, property.Value));
            }

            property = GetPropertyModifier(AggregateField.damage_thermal);

            if (property.HasValue)
            {
                damageModifier.Modify(ref property);
                result.Add(new Damage(DamageType.Thermal, property.Value));
            }

            property = GetPropertyModifier(AggregateField.damage_kinetic);

            if (property.HasValue)
            {
                damageModifier.Modify(ref property);
                result.Add(new Damage(DamageType.Kinetic, property.Value));
            }

            property = GetPropertyModifier(AggregateField.damage_explosive);

            if (!property.HasValue) 
                return result;

            damageModifier.Modify(ref property);
            result.Add(new Damage(DamageType.Explosive, property.Value));

            return result;
        }

        private void UpdateCleanDamages()
        {
            _cleanDamages = null;
        }

        public override void ModifyOptimalRange(ref ItemPropertyModifier property)
        {
            var optimalRangeMod = _optimalRangeModifier.ToPropertyModifier();
            optimalRangeMod.Modify(ref property);
        }

        public ItemPropertyModifier GetExplosionRadius()
        {
            return GetPropertyModifier(AggregateField.explosion_radius);
        }
    }
}