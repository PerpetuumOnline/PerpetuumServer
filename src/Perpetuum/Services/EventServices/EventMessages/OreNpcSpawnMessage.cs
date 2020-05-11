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
        private readonly int _zoneId;
        private readonly MineralNode _node;
        private readonly OreNodeState _state;

        public OreNpcSpawnMessage(MineralNode node, int zoneID, OreNodeState state)
        {
            _zoneId = zoneID;
            _node = node;
            _state = state;
        }

        public OreNodeState GetOreNodeState()
        {
            return _state;
        }

        public int GetZoneID()
        {
            return _zoneId;
        }

        public MineralNode GetMineralNode()
        {
            return _node;
        }
    }
}
