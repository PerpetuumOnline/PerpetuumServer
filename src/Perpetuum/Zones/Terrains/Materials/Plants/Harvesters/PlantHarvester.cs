using System.Collections.Generic;
using Perpetuum.Items;

namespace Perpetuum.Zones.Terrains.Materials.Plants.Harvesters
{
    public class PlantHarvester : IPlantHarvester
    {
        private readonly IZone _zone;
        private readonly double _amountModifier;
        private readonly RareMaterialHandler _rareMaterialHandler;
        private readonly MaterialHelper _materialHelper;

        public delegate IPlantHarvester Factory(IZone zone, double amountModifier);

        public PlantHarvester(IZone zone, double amountModifier,RareMaterialHandler rareMaterialHandler,MaterialHelper materialHelper)
        {
            _zone = zone;
            _amountModifier = amountModifier;
            _rareMaterialHandler = rareMaterialHandler;
            _materialHelper = materialHelper;
        }

        public IEnumerable<ItemInfo> HarvestPlant(Position position)
        {
            var result = new List<ItemInfo>();

            _zone.Terrain.Plants.UpdateValue(position,pi =>
            {
                pi.type.ThrowIfEqual(PlantType.NotDefined, ErrorCodes.NoPlantOnTile);
                var plantRule = _zone.Configuration.PlantRules.GetPlantRule(pi.type).ThrowIfNull(ErrorCodes.InvalidPlant);

                pi.material.ThrowIfLessOrEqual((byte)0, ErrorCodes.NoMaterialOnThePlant);
                pi.material--;

                if (pi.material <= 0)
                {
                    //elfogyott a material a novenybol
                    //megoljuk erobol
                    pi.Clear();
                    pi.state = 1; //force kidoles :)
                    //kill the blocking as well
                    _zone.Terrain.ClearPlantBlocking(position);
                }

                //resulting material
                var quantity = GetHarvestedAmountPerCycle(plantRule);
                result.Add(new ItemInfo(plantRule.FruitDefinition, quantity));
                result.AddRange(_rareMaterialHandler.GenerateRareMaterials(plantRule.FruitDefinition));
                return pi;
            });

            return result;
        }

        private int GetHarvestedAmountPerCycle(PlantRule plantRule)
        {
            double amount;

            switch (plantRule.Type)
            {
                case PlantType.SlimeRoot:
                case PlantType.ElectroPlant:
                case PlantType.RustBush:
                case PlantType.TreeIron:
                {
                    var m = _materialHelper.GetMaterialInfo(plantRule.FruitMaterialName.ToMaterialType());
                    amount = m.Amount;
                    break;
                }
                default:
                throw new PerpetuumException(ErrorCodes.PlantNotHarvestable);
            }

            return (int) (amount * _amountModifier);
        }
    }

}
