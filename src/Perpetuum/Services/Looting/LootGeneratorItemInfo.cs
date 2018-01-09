using Perpetuum.Items;

namespace Perpetuum.Services.Looting
{
    public class LootGeneratorItemInfo
    {
        public readonly ItemInfo item;
        public readonly bool damaged;
        public readonly double probability;

        public LootGeneratorItemInfo(ItemInfo item,bool damaged,double probability)
        {
            this.item = item;
            this.damaged = damaged;
            this.probability = probability;
        }
    }
}