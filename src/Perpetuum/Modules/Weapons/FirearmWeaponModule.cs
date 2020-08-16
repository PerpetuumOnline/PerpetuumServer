using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;

namespace Perpetuum.Modules.Weapons
{
    public class FirearmWeaponModule : WeaponModule
    {

        public FirearmWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
        }

        protected override bool CheckAccuracy(Unit victim)
        {
            return false;
        }

        protected override IDamageBuilder GetDamageBuilder()
        {
            return base.GetDamageBuilder().WithExplosionRadius(Accuracy.Value);
        }

    }
}
