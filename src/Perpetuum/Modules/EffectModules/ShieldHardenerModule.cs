namespace Perpetuum.Modules.EffectModules
{
/*

    public class ShieldHardenerModule : EffectModule
    {
        private readonly IProperty _propertyEffectShieldAbsorbtionModifier;

        public ShieldHardenerModule()
        {
            _propertyEffectShieldAbsorbtionModifier = new ModuleProperty(this,AggregateField.effect_shield_absorbtion_modifier);
        }

        public override void UpdateAllProperties()
        {
            base.UpdateAllProperties();
            _propertyEffectShieldAbsorbtionModifier.Calculate();
        }

        public override IList<Property> GetProperties()
        {
            var result = base.GetProperties();

            result.Add(_propertyEffectShieldAbsorbtionModifier.ToProperty());

            return result;
        }

        protected override IEffect CreateEffect(Unit target)
        {
            var e = EffectFactory.instance.Create(EffectType.effect_shield_hardener, target);
            e.AddPropertyModifier(_propertyEffectShieldAbsorbtionModifier.ToProperty());
            return e;
        }
    }
 */
}