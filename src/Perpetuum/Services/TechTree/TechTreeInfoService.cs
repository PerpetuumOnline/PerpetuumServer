using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public class TechTreeInfoService : ITechTreeInfoService
    {
        public IDictionary<TechTreeGroup, TechTreeGroupInfo> GetGroupInfos()
        {
            return Db.Query().CommandText("select * from techtreegroups").Execute().Select(r => new TechTreeGroupInfo
            {
                @group = r.GetValue<TechTreeGroup>("id"),
                enablerExtensionId = r.GetValue<int>("enablerExtensionId"),
                displayOrder = r.GetValue<int>("displayOrder")
            }).ToDictionary(g => g.group);
        }

        public IDictionary<int, TechTreeNode> GetNodes()
        {
            var prices = GetPrices();

            return Db.Query().CommandText("select * from techtree").Execute().Select(r =>
            {
                var node = TechTreeNode.CreateFromDataRecord(r);
                node.Prices = prices[node.Definition].ToArray();
                return node;
            }).ToDictionary(n => n.Definition);
        }

        private const int CORPORATION_PRICE_MULTIPLIER = 3;

        public int CorporationPriceMultiplier => CORPORATION_PRICE_MULTIPLIER;

        private static ILookup<int, Points> GetPrices()
        {
            return Db.Query().CommandText("select * from techtreenodeprices")
                          .Execute()
                          .ToLookup(r => r.GetValue<int>("definition"),
                                    r => new Points(r.GetValue<TechTreePointType>("pointtype"), r.GetValue<int>("amount")));
        }
    }
}