using System.Collections.Generic;

namespace Perpetuum.Services.TechTree
{
    public interface ITechTreeService
    {
        IEnumerable<TechTreeNode> GetUnlockedNodes(long owner);
        void NodeUnlocked(long owner, TechTreeNode node);
        void AddInfoToDictionary(long owner, IDictionary<string, object> dictionary);
    }
}