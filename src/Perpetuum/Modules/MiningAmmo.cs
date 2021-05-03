using System.Collections.Generic;
using Perpetuum.ExportedTypes;

using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Modules
{
    public class MiningAmmo : Ammo
    {
        private static readonly Dictionary<MaterialType, MiningAmmoModifier> _modifiers;
        public readonly ItemProperty miningCycleTimeModifier;

        static MiningAmmo()
        {
            _modifiers = new Dictionary<MaterialType, MiningAmmoModifier>
                             {
                                 {MaterialType.FluxOre, new MiningAmmoModifier(AggregateField.mining_cycle_time_flux_modifier, AggregateField.mining_amount_flux_modifier)},
                                 {MaterialType.Epriton, new MiningAmmoModifier(AggregateField.mining_cycle_time_epriton_modifier, AggregateField.mining_amount_epriton_modifier)},
                                 {MaterialType.Titan, new MiningAmmoModifier(AggregateField.mining_cycle_time_titan_modifier, AggregateField.mining_amount_titan_modifier)},
                                 {MaterialType.Stermonit, new MiningAmmoModifier(AggregateField.mining_cycle_time_stermonit_modifier, AggregateField.mining_amount_stermonit_modifier)},
                                 {MaterialType.Crude, new MiningAmmoModifier(AggregateField.mining_cycle_time_crude_modifier, AggregateField.mining_amount_crude_modifier)},
                                 {MaterialType.Imentium, new MiningAmmoModifier(AggregateField.mining_cycle_time_imentium_modifier, AggregateField.mining_amount_imentium_modifier)},
                                 {MaterialType.Liquizit, new MiningAmmoModifier(AggregateField.mining_cycle_time_liquizit_modifier, AggregateField.mining_amount_liquizit_modifier)},
                                 {MaterialType.Silgium, new MiningAmmoModifier(AggregateField.mining_cycle_time_silgium_modifier, AggregateField.mining_amount_silgium_modifier)},
                                 {MaterialType.Gammaterial, new MiningAmmoModifier(AggregateField.mining_cycle_time_gammaterial_modifier, AggregateField.mining_amount_gammaterial_modifier)}
                             };
        }

        public MiningAmmo() 
        {
            miningCycleTimeModifier = new MiningCycleTimeModifierProperty(this);
            AddProperty(miningCycleTimeModifier);
        }

        public MaterialType MaterialType => ED.Options.MineralLayer.ToMaterialType();

        private static bool TryGetMiningAmmoModifier(MaterialType materialType,out MiningAmmoModifier miningAmmoModifier)
        {
            return _modifiers.TryGetValue(materialType,out miningAmmoModifier);
        }

        private struct MiningAmmoModifier
        {
            public readonly AggregateField cycleTimeModifier;
            public readonly AggregateField amountModifier;

            public MiningAmmoModifier(AggregateField cycleTimeModifier,AggregateField amountModifier)
            {
                this.cycleTimeModifier = cycleTimeModifier;
                this.amountModifier = amountModifier;
            }
        }

        public void ApplyMiningAmountModifier(ref ItemPropertyModifier propertyModifier)
        {
            MiningAmmoModifier modifier;
            if (!TryGetMiningAmmoModifier(MaterialType, out modifier)) 
                return;

            var parentRobot = GetParentRobot();
            if (parentRobot == null)
                return;

            var amountMod = parentRobot.GetPropertyModifier(modifier.amountModifier);
            amountMod.Modify(ref propertyModifier);
        }

        private class MiningCycleTimeModifierProperty : AmmoProperty<MiningAmmo>
        {
            public MiningCycleTimeModifierProperty(MiningAmmo ammo) : base(ammo,AggregateField.mining_cycle_time_modifier)
            {
            }

            protected override double CalculateValue()
            {
                var parentRobot = ammo.GetParentRobot();
                if (parentRobot == null)
                    return 1.0;

                if (!TryGetMiningAmmoModifier(ammo.MaterialType, out MiningAmmoModifier modifier))
                    return 1.0;

                var m = parentRobot.GetPropertyModifier(modifier.cycleTimeModifier);
                return m.Value;
            }
        }
    }



}