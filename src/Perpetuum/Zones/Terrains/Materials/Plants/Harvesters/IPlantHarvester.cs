using System.Collections.Generic;
using Perpetuum.Items;

namespace Perpetuum.Zones.Terrains.Materials.Plants.Harvesters
{
    public interface IPlantHarvester
    {
        IEnumerable<ItemInfo> HarvestPlant(Position position);
    }
}