using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.TechTree
{
    public class CachedTechTreeService : ITechTreeService
    {
        private readonly ConcurrentDictionary<long,TechTreeNode[]> _nodes = new ConcurrentDictionary<long, TechTreeNode[]>();
        private readonly ITechTreeService _techTreeService;

        public void NodeUnlocked(long owner, TechTreeNode node)
        {
            _nodes.Remove(owner);
        }

        public CachedTechTreeService(ITechTreeService techTreeService)
        {
            _techTreeService = techTreeService;
        }

        public IEnumerable<TechTreeNode> GetUnlockedNodes(long owner)
        {
            return _nodes.GetOrAdd(owner, (o) => _techTreeService.GetUnlockedNodes(o).ToArray());
        }

        public void AddInfoToDictionary(long owner, IDictionary<string, object> dictionary)
        {
            _techTreeService.AddInfoToDictionary(owner, dictionary);
        }
    }
}