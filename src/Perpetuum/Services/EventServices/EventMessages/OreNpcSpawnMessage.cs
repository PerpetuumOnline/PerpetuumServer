using Perpetuum.Zones.Terrains.Materials.Minerals;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public enum OreNodeState
    {
        Updated,
        Removed
    }

    public class OreNpcSpawnMessage : EventMessage
    {
        public OreNodeState NodeState { get; }
        public MineralNode Node { get; }
        public int ZoneId { get; }

        public OreNpcSpawnMessage(MineralNode node, int zoneID, OreNodeState state)
        {
            ZoneId = zoneID;
            Node = node;
            NodeState = state;
        }
    }
}
