using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;

namespace Perpetuum.Services.Trading
{
    public class TradeItem
    {
        public readonly long itemEid;
        private readonly IDictionary<string, object> _info;

        public TradeItem(Item item)
        {
            itemEid = item.Eid;
            ItemInfo = item.ItemInfo;
            _info = ItemTradeInfoBuilder.GetTradeInfo(item);
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.definition,ItemInfo.Definition},
                {k.quantity,ItemInfo.Quantity},
                {k.info, _info}
            };
        }

        public ItemInfo ItemInfo { get; private set; }

        private class ItemTradeInfoBuilder : IEntityVisitor<Item>, IEntityVisitor<CalibrationProgram>
        {
            private Dictionary<string, object> _tradeInfo;

            private void AddItemTradeInfo(Item item)
            {
                _tradeInfo = item.BaseInfoToDictionary();
            }

            public void Visit(Item item)
            {
                AddItemTradeInfo(item);
            }

            public void Visit(CalibrationProgram calibrationProgram)
            {
                AddItemTradeInfo(calibrationProgram);
                _tradeInfo[k.materialEfficiency] = calibrationProgram.MaterialEfficiencyPoints;
                _tradeInfo[k.timeEfficiency] = calibrationProgram.TimeEfficiencyPoints;
            }

            public static Dictionary<string, object> GetTradeInfo(Item item)
            {
                var builder = new ItemTradeInfoBuilder();
                item.AcceptVisitor(builder);
                return builder._tradeInfo;
            }
        }
    }
}