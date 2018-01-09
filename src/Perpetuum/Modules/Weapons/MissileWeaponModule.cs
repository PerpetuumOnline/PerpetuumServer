using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;

namespace Perpetuum.Modules.Weapons
{
    public class MissileWeaponModule : WeaponModule
    {
        private readonly ItemProperty _propertyExplosionRadius;
        public readonly ModuleProperty MissileRangeModifier;

        public MissileWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            _propertyExplosionRadius = new ExplosionRadiusProperty(this);
            AddProperty(_propertyExplosionRadius);
            MissileRangeModifier = new ModuleProperty(this, AggregateField.module_missile_range_modifier);
            MissileRangeModifier.AddEffectModifier(AggregateField.effect_missile_range_modifier);
            AddProperty(MissileRangeModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.explosion_radius:
                case AggregateField.explosion_radius_modifier:
                    {
                        _propertyExplosionRadius.Update();
                        return;
                    }
                case AggregateField.module_missile_range_modifier:
                case AggregateField.effect_missile_range_modifier:
                    {
                        MissileRangeModifier.Update();
                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        protected override bool CheckAccuracy(Unit victim)
        {
            var rnd = FastRandom.NextDouble();
            var isMiss = rnd > ParentRobot.MissileHitChance;
            return isMiss;
        }

        protected override IDamageBuilder GetDamageBuilder()
        {
            return base.GetDamageBuilder().WithExplosionRadius(_propertyExplosionRadius.Value);
        }

        private class ExplosionRadiusProperty : ModuleProperty
        {
            private readonly MissileWeaponModule _module;

            public ExplosionRadiusProperty(MissileWeaponModule module) : base(module,AggregateField.explosion_radius)
            {
                _module = module;
            }

            protected override double CalculateValue()
            {
                var ammo = (WeaponAmmo)_module.GetAmmo();
                if (ammo == null) 
                    return 0.0;

                var property = ammo.GetExplosionRadius();
                _module.ApplyRobotPropertyModifiers(ref property);
                return property.Value;
            }
        }
    }
}