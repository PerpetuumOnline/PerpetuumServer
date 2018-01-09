using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Terrains.Materials.Plants.ExtensionsMethods
{
    public static class PlantInfoExtensions
    {
        public static IDictionary<PlantType, int> CountPlants(this IEnumerable<PlantInfo> plantInfos)
        {
            var plantAmount = new Dictionary<PlantType, int>();

            foreach (var plantInfo in plantInfos.Where(plantInfo => plantInfo.type != PlantType.NotDefined))
            {
                plantAmount.AddOrUpdate(plantInfo.type, 1, c => ++c);
            }

            return plantAmount;
        }
    }
}
