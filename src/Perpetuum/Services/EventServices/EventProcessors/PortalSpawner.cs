using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class PortalSpawner : EventProcessor
    {
        private readonly IEntityServices _entityServices;
        private readonly IZoneManager _zoneManager;

        public override EventType Type => EventType.PortalSpawn;

        public PortalSpawner(IEntityServices entityServices, IZoneManager zoneManager)
        {
            _entityServices = entityServices;
            _zoneManager = zoneManager;
        }

        private bool ValidateMessage(SpawnPortalMessage msg)
        {
            if (msg.RiftConfig == null || !_zoneManager.ContainsZone(msg.SourceZone))
            {
                return false;
            }
            else if (msg.SourcePosition == Position.Empty)
            {
                return false;
            }
            return msg.SourcePosition.IsValid(_zoneManager.GetZone(msg.SourceZone).Size);
        }

        private Position TryGetValidPosition(SpawnPortalMessage msg)
        {
            var zone = _zoneManager.GetZone(msg.SourceZone);
            if (zone.IsWalkable(msg.SourcePosition))
            {
                return msg.SourcePosition;
            }
            var finder = new ClosestWalkablePositionFinder(zone, msg.SourcePosition);
            if (finder.Find(out Position result))
            {
                return result;
            }
            return Position.Empty;
        }

        public override void HandleMessage(IEventMessage value)
        {
            if (value is SpawnPortalMessage msg)
            {
                if (!ValidateMessage(msg))
                {
                    Logger.Warning($"SpawnPortalMessage was not valid!\n{msg}");
                    return;
                }

                var spawnPos = TryGetValidPosition(msg);
                if (spawnPos == Position.Empty)
                {
                    Logger.Warning($"SpawnPortalMessage's Position was not valid and could not be fixed\n{msg}");
                    return;
                }

                CustomRiftSpawner.TrySpawnRift(msg.RiftConfig, _zoneManager, msg.SourceZone, spawnPos, () =>
                {
                    return (StrongholdEntryRift)_entityServices.Factory.CreateWithRandomEID(DefinitionNames.TARGETTED_RIFT);
                });
            }
        }
    }
}
