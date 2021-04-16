using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Zones;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class PortalSpawner : EventProcessor
    {
        private readonly IEntityServices _entityServices;
        private readonly IZoneManager _zoneManager;

        public PortalSpawner(IEntityServices entityServices, IZoneManager zoneManager)
        {
            _entityServices = entityServices;
            _zoneManager = zoneManager;
        }

        private bool ValidateMessage(SpawnPortalMessage msg)
        {
            if (msg.RiftConfig == null || !_zoneManager.ContainsZone(msg.SourceZone))
                return false;
            if (!_zoneManager.GetZone(msg.SourceZone).IsWalkable(msg.SourcePosition))
                return false;
            return true;
        }

        public override void HandleMessage(EventMessage value)
        {
            if (value is SpawnPortalMessage msg)
            {
                if (!ValidateMessage(msg))
                    return;


                CustomRiftSpawner.TrySpawnRift(msg.RiftConfig, _zoneManager, msg.SourceZone, msg.SourcePosition, () =>
                {
                    return (StrongholdEntryRift)_entityServices.Factory.CreateWithRandomEID(DefinitionNames.TARGETTED_RIFT);
                });
            }
        }
    }
}
