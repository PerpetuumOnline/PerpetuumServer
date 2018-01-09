using System;
using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public class CachedTechTreeInfoService : ITechTreeInfoService
    {
        private readonly ITechTreeInfoService _techTreeInfoService;
        private readonly Lazy<IDictionary<TechTreeGroup, TechTreeGroupInfo>> _groupInfos;
        private readonly Lazy<IDictionary<int, TechTreeNode>> _nodes;

        public CachedTechTreeInfoService(ITechTreeInfoService techTreeInfoService)
        {
            _techTreeInfoService = techTreeInfoService;
            _nodes = new Lazy<IDictionary<int, TechTreeNode>>(techTreeInfoService.GetNodes);
            _groupInfos = new Lazy<IDictionary<TechTreeGroup, TechTreeGroupInfo>>(techTreeInfoService.GetGroupInfos);
        }

        public IDictionary<TechTreeGroup, TechTreeGroupInfo> GetGroupInfos()
        {
            return _groupInfos.Value;
        }

        public IDictionary<int, TechTreeNode> GetNodes()
        {
            return _nodes.Value;
        }

        public int CorporationPriceMultiplier => _techTreeInfoService.CorporationPriceMultiplier;
    }
}