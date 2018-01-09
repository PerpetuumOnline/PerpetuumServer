using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using Perpetuum.Log;
using Perpetuum.Zones.Terrains.Materials.Minerals.Actions;
using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public abstract class MineralLayer : ActiveLayer,IMaterialLayer
    {
        private readonly IMineralConfiguration _configuration;
        private readonly IMineralNodeRepository _nodeRepository;
        private readonly IMineralNodeGeneratorFactory _nodeGeneratorFactory;
        private MineralNode[] _nodes = new MineralNode[0];

        protected MineralLayer(int width,int height,IMineralConfiguration configuration,IMineralNodeRepository nodeRepository,IMineralNodeGeneratorFactory nodeGeneratorFactory) : base(LayerType.Material,width,height)
        {
            _configuration = configuration;
            _nodeRepository = nodeRepository;
            _nodeGeneratorFactory = nodeGeneratorFactory;
        }

        public IMineralNodeRepository NodeRepository => _nodeRepository;

        public virtual void AcceptVisitor(MineralLayerVisitor visitor)
        {
            visitor.VisitMineralLayer(this);
        }

        public void LoadMineralNodes()
        {
            var nodes = NodeRepository.GetAll();

            var deleted = 0;

            foreach (var node in nodes)
            {
                var totalAmount = node.GetTotalAmount();
                var threshold = (double)totalAmount / Configuration.TotalAmountPerNode;
                if (threshold <= 0.01)
                {
                    NodeRepository.Delete(node);
                    deleted++;
                    continue;
                }

                AddNode(node);
            }

            WriteLog($"Nodes loaded. ({_nodes.Length})");
            WriteLog($"Nodes deleted. ({deleted})");

            if (_nodes.Length < _configuration.MaxNodes)
            {
                for (var i = 0; i < _configuration.MaxNodes - _nodes.Length; i++)
                {
                    GenerateNewNode();
                }
            }
        }

        public void GenerateNewNode()
        {
            var generator = _nodeGeneratorFactory.Create();
            if (generator == null)
                return;

            generator.Radius = (int) Math.Sqrt(_configuration.MaxTilesPerNode / Math.PI) * 2;
            generator.MaxTiles = _configuration.MaxTilesPerNode;
            generator.TotalAmount = _configuration.TotalAmountPerNode;
            generator.MinThreshold = _configuration.MinThreshold;

            RunAction(new GenerateMineralNode(generator));
        }

        public List<MineralNode> GetNodesWithinRange(Point location, int range)
        {
            var area = Area.FromRadius(location.X, location.Y, range);
            return GetNodesByArea(area);
        }

        public List<MineralNode> GetNodesByArea(Area area)
        {
            var nodes = new List<MineralNode>();

            foreach (var node in _nodes)
            {
                if ( node.Area.IntersectsWith(area) )
                    nodes.Add(node);
            }

            return nodes;
        }

        public MineralNode GetNearestNode(Point p)
        {
            MineralNode nearestNode = null;
            var nearestDistSq = double.MaxValue;

            foreach (var node in _nodes)
            {
                var distSqr = node.Area.SqrDistance(p);
                if (distSqr >= nearestDistSq)
                    continue;

                nearestNode = node;
                nearestDistSq = distSqr;
            }

            return nearestNode;
        }

        public MineralNode[] Nodes
        {
            get { return _nodes; }
        }

        public MaterialType Type
        {
            get { return Configuration.Type; }
        }

        public IMineralConfiguration Configuration
        {
            get { return _configuration; }
        }

        public void AddNode(MineralNode node)
        {
            ImmutableInterlocked.Update(ref _nodes, nodes =>
            {
                var result = new List<MineralNode>(nodes) {node};
                return result.ToArray();
            });

            node.Updated += OnNodeUpdated;
            node.Expired += OnNodeExpired;
        }

        public void RemoveNode(MineralNode node)
        {
            ImmutableInterlocked.Update(ref _nodes, nodes =>
            {
                var result = new List<MineralNode>();

                foreach (var n in nodes)
                {
                    if ( n == node )
                        continue;

                    result.Add(n);
                }

                return result.ToArray();
            });
        }

        private void OnNodeUpdated(MineralNode node)
        {
            RunAction(new SaveMineralNode(node));
        }

        private void OnNodeExpired(MineralNode node)
        {
            RunAction(new DeleteMineralNode(node));
        }

        [CanBeNull]
        public MineralNode GetNode(Point p)
        {
            MineralNode node;
            if (!TryGetNode(p, out node))
                return null;

            return node;
        }

        public bool TryGetNode(Point p, out MineralNode node)
        {
            return TryGetNode(p.X, p.Y, out node);
        }

        private bool TryGetNode(int x, int y, out MineralNode node)
        {
            foreach (var n in _nodes)
            {
                if (!n.Area.Contains(x, y))
                    continue;

                node = n;
                return true;
            }

            node = null;
            return false;
        }

        public override void Update(TimeSpan time)
        {
            foreach (var node in _nodes)
            {
                node.Update(time);
            }

            base.Update(time);
        }

        public void WriteLog(string message)
        {
            Logger.Info($"Mineral ({_configuration.ZoneId}:{Type}) {message}");
        }

        public bool HasMineral(Point location)
        {
            var node = GetNode(location);
            if (node == null)
                return false;

            return node.HasValue(location);
        }

        public MineralNode CreateNode(Area area)
        {
            return new MineralNode(Type,area);
        }
    }
}