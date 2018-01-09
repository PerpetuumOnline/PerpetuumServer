using System;
using GenXY.Framework;
using Perpetuum.Items;

namespace Perpetuum.Zones.LootContainers
{
    public sealed class LootItem
    {
        public readonly Guid id;

        public ItemInfo ItemInfo { get; private set; }

        public int Quantity
        {
            get { return ItemInfo.Quantity; }
            set
            {
                var itemInfo = ItemInfo;
                itemInfo.Quantity = value;
                ItemInfo = itemInfo;
            }
        }

        public LootItem(Guid id,ItemInfo itemInfo)
        {
            this.id = id;
            ItemInfo = itemInfo;
        }

        public void AppendToPacket(Packet packet)
        {
            packet.AppendGuid(id);
            packet.AppendInt(ItemInfo.Definition);
            packet.AppendInt(ItemInfo.Quantity);
            packet.AppendDouble(ItemInfo.Volume);
            packet.AppendDouble(ItemInfo.Health);
            packet.AppendByte(ItemInfo.IsRepackaged.ToByte());
        }

        public override string ToString()
        {
            return $"Id: {id}, Item:: {ItemInfo}";
        }
    }
}