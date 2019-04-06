using System.Collections.Generic;

namespace Perpetuum.Services.Looting
{
    public class NullLootGenerator : ILootGenerator
    {
        public IEnumerable<LootItem> Generate()
        {
            yield break;
        }

        public IReadOnlyCollection<LootGeneratorItemInfo> GetInfos()
        {
            return null;
        }
    }
}