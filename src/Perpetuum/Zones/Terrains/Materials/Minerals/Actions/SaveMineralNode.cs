namespace Perpetuum.Zones.Terrains.Materials.Minerals.Actions
{
    public class SaveMineralNode : ILayerAction
    {
        private readonly MineralNode _node;

        public SaveMineralNode(MineralNode node)
        {
            _node = node;
        }

        public void Execute(ILayer layer)
        {
            var mineralLayer = (MineralLayer)layer;
            mineralLayer.NodeRepository.Update(_node);
            mineralLayer.WriteLog($"Node saved. X = {_node.Area.X1} Y = {_node.Area.Y1}");
        }
    }
}