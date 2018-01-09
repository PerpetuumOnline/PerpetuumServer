using System.Collections.Generic;

namespace Perpetuum.Services.ProductionEngine
{
    public struct ProductionItemInfo
    {
        public int definition;
        public int nominalAmount;
        public int realAmount;

        public ProductionItemInfo(int definition, int nominalAmount, int realAmount)
        {
            this.definition = definition;
            this.nominalAmount = nominalAmount;
            this.realAmount = realAmount;
        }


        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.definition, definition},
                {k.nominal, nominalAmount},
                {k.real, realAmount}
            };
        }
    }
}