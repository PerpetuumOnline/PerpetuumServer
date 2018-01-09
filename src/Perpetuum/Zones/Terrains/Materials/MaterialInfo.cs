using Perpetuum.EntityFramework;
using Perpetuum.Items;

namespace Perpetuum.Zones.Terrains.Materials
{
    public class MaterialInfo
    {
        public static readonly MaterialInfo None = new MaterialInfo
        {
            Type = MaterialType.Undefined,
            EntityDefault = EntityDefault.None,
            Amount = 0,
            EnablerExtensionRequired = false
        };

        public MaterialType Type { get; set; }
        public EntityDefault EntityDefault { get; set; }
        public int Amount { get; set; }
        public bool EnablerExtensionRequired { get; set; }

        public ItemInfo ToItem(int quantity)
        {
            return new ItemInfo(EntityDefault.Definition, quantity);
        }
    }
}