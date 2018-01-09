using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Items
{
    public class LotteryItem : Item
    {
        private readonly IEntityDefaultReader _entityDefaultReader;
        private static ILookup<EntityDefault, WeightInfo> _weights;

        private class WeightInfo
        {
            public readonly EntityDefault lotteryItem;
            public readonly CategoryFlags categoryFlags;
            public readonly TierInfo tier;
            public readonly double weight;

            public WeightInfo(EntityDefault lotteryItem, CategoryFlags categoryFlags, TierInfo tier, double weight)
            {
                this.lotteryItem = lotteryItem;
                this.categoryFlags = categoryFlags;
                this.tier = tier;
                this.weight = weight;
            }
        }

        public LotteryItem(IEntityDefaultReader entityDefaultReader) 
        {
            _entityDefaultReader = entityDefaultReader;

            if (_weights == null)
            {
                _weights = Db.Query().CommandText("select * from lotteryitemweights")
                              .Execute()
                              .Select(r =>
                              {
                                  var lotteryDefinition = r.GetValue<int>("lotterydefinition");
                                  var categoryFlags = r.GetValue<CategoryFlags>("categoryflags");
                                  var tierType = (TierType)r.GetValue<int>("tiertype");
                                  var tierLevel = r.GetValue<int>("tierlevel");
                                  var weight = r.GetValue<double>("weight");
                                  var tier = new TierInfo(tierType,tierLevel);

                                  return new WeightInfo(EntityDefault.Get(lotteryDefinition),categoryFlags,tier,weight);
                              }).ToLookup(i => i.lotteryItem);
            }

        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public EntityDefault PickRandomItem()
        {
            var weights = _weights.GetOrEmpty(ED);
            var randomCf = weights.Select(w => w.categoryFlags).Distinct().RandomElement();
            var tierWeights = weights.Where(w => w.categoryFlags == randomCf).ToDictionary(w => w.tier, w => w.weight);

            var randomItem = _entityDefaultReader.GetAll().GetByCategoryFlags(randomCf).RandomElementByWeight(ed => tierWeights.GetOrDefault(ed.Tier));
            return randomItem;
        }
    }
}
