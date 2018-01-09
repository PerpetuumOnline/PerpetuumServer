using System.Data;
using Perpetuum.Builders;
using Perpetuum.Data;
using Perpetuum.Services.Looting;

namespace Perpetuum.Zones.Artifacts
{
    public interface IArtifactLoot
    {
        double Chance { get; }
        IBuilder<LootItem> GetLootItemBuilder();
    }

    /// <summary>
    /// Describes one loot item can be found in a discovered artifact
    /// </summary>
    public class ArtifactLoot : IArtifactLoot
    {
        private int Definition { get; set; }
        private IntRange Quantity { get; set; }
        public double Chance { get; private set; }

        public IBuilder<LootItem> GetLootItemBuilder()
        {
            return LootItemBuilder.Create(Definition)
                .SetQuantity(FastRandom.NextInt(Quantity))
                .SetRepackaged(Packed);
        }

        private bool Packed { get; set; }

        public ArtifactLoot(IDataRecord record)
        {
            Definition = record.GetValue<int>("definition");
            Quantity = new IntRange(record.GetValue<int>("minquantity"),record.GetValue<int>("maxquantity"));
            Chance = record.GetValue<double>("chance");
            Packed = record.GetValue<bool>("packed");
        }
    }
}