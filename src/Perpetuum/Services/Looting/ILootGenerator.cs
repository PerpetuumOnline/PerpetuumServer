using System.Collections.Generic;

namespace Perpetuum.Services.Looting
{
    public interface ILootGenerator
    {
        IEnumerable<LootItem> Generate();
    }
}