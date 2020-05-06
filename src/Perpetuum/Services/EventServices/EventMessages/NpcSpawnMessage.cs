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
        private int _zoneId;
        private MineralNode _node;
        private OreNodeState _state;

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
