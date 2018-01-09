using System.Collections.Generic;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.Looting
{
    public interface ILootService
    {
        IEnumerable<LootGeneratorItemInfo> GetNpcLootInfos(int definition);
        IEnumerable<LootGeneratorItemInfo> GetFlockLootInfos(int flockID);
        IEnumerable<LootGeneratorItemInfo> GetIntrusionLootInfos(Outpost outpost,SAP sap);
    }
}