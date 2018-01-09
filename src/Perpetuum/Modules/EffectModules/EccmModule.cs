namespace Perpetuum.Modules.EffectModules
{
/*

    public class EccmModule : EffectModule
    {
        private readonly ModuleProperty _propertyEffectSensorStrengthModifier;
        private readonly ModuleProperty _propertyEffectMassiveness;
        private readonly ModuleProperty _propertyEffectLockResist;

        public EccmModule()
        {
            _propertyEffectSensorStrengthModifier = new ModuleProperty(this, AggregateField.effect_sensor_strength_modifier);
            _propertyEffectMassiveness = new ModuleProperty(this, AggregateField.effect_massiveness);
            _propertyEffectLockResist = new ModuleProperty(this, AggregateField.effect_lock_resist);
        }

        public override void UpdateAllProperties()
        {
            base.UpdateAllProperties();
            _propertyEffectSensorStrengthModifier.Calculate();
            _propertyEffectMassiveness.Calculate();
            _propertyEffectLockResist.Calculate();
        }

        public override IList<Property> GetProperties()
        {
            var result = base.GetProperties();

            result.Add(_propertyEffectSensorStrengthModifier.ToProperty());
            result.Add(_propertyEffectMassiveness.ToProperty());
            result.Add(_propertyEffectLockResist.ToProperty());

            return result;
        }

        protected override IEffect CreateEffect(Unit target)
        {
            var effect = (CoTEffect)EffectFactory.instance.Create(EffectType.effect_eccm,parentRobot);
            effect.SetCorePerTick(CoTEffect.CalculateCorePerTickByModule(this));
            
            effect.AddPropertyModifier(_propertyEffectSensorStrengthModifier.ToProperty());
            effect.AddPropertyModifier(_propertyEffectMassiveness.ToProperty());
            effect.AddPropertyModifier(_propertyEffectLockResist.ToProperty());

            return effect;
        }

        protected override ErrorCodes OnAction(ref bool consumeCore, ILock currentLock)
        {
            ErrorCodes err;

            if ((err = base.OnAction(ref consumeCore, currentLock)) == ErrorCodes.NoError )
            {
                parentRobot.GetEffectHandler().RemoveEffectsByCategory(EffectCategory.effcat_eccmcureable_effects);
            }
            
            return err;
        }

    }
 */
}