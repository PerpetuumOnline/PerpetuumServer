namespace Perpetuum.Zones.Terrains.Materials.Minerals.Actions
{
    public class DeleteMineralNode : ILayerAction
    {
        private readonly MineralNode _node;

        public DeleteMineralNode(MineralNode node)
        {
            _node = node;
        }

        public void Execute(ILayer layer)
        {
            var mineralLayer = (MineralLayer) layer;
            mineralLayer.NodeRepository.Delete(_node);
            mineralLayer.RemoveNode(_node);
            mineralLayer.WriteLog($"Node deleted. X = {_node.Area.X1} Y = {_node.Area.Y1}");

            mineralLayer.GenerateNewNode();
        }
    }
}