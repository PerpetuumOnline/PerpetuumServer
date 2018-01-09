using System.Collections.Generic;
using Perpetuum.Services.Looting;

namespace Perpetuum.Zones.Artifacts.Generators.Loot
{
    public class ArtifactLootItems
    {
        public Position Position { get; private set; }
        public IEnumerable<LootItem> LootItems { get; private set; }

        public ArtifactLootItems(Position position, IEnumerable<LootItem> lootItems)
        {
            Position = position;
            LootItems = lootItems;
        }
    }
}