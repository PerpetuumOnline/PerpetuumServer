using System.Collections.Generic;

namespace Perpetuum.Services.Looting
{
    public interface ILootGenerator
    {
        IEnumerable<LootItem> Generate();

        IReadOnlyCollection<LootGeneratorItemInfo> GetInfos();
    }

    public interface ISplittableLootGenerator
    {
        List<ILootGenerator> GetGenerators(int splitCount);
    }
}