using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public interface ITechTreeInfoService
    {
        IDictionary<TechTreeGroup, TechTreeGroupInfo> GetGroupInfos();
        IDictionary<int, TechTreeNode> GetNodes();
        int CorporationPriceMultiplier { get; }
    }
}