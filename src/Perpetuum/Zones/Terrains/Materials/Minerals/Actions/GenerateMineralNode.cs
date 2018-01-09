using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;

namespace Perpetuum.Zones.Terrains.Materials.Minerals.Actions
{
    public class GenerateMineralNode : MineralLayerVisitor,ILayerAction
    {
        private readonly IMineralNodeGenerator _nodeGenerator;

        public GenerateMineralNode(IMineralNodeGenerator nodeGenerator)
        {
            _nodeGenerator = nodeGenerator;
        }

        public override void VisitGravelLayer(GravelLayer layer)
        {
            // nem generalunk
        }

        public override void VisitMineralLayer(MineralLayer layer)
        {
            var node = _nodeGenerator.Generate(layer);
            if (node == null)
                return;

            layer.NodeRepository.Insert(node);
            layer.AddNode(node);
            layer.WriteLog($"Node generated. X = {node.Area.X1} Y = {node.Area.Y1}");
        }

        public void Execute(ILayer layer)
        {
            var mineralLayer = layer as MineralLayer;
            mineralLayer?.AcceptVisitor(this);
        }
    }
}