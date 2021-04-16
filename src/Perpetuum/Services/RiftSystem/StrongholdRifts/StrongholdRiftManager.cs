using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;

namespace Perpetuum.Services.RiftSystem.StrongholdRifts
{
    /// <summary>
    /// RiftManager for Stronghold
    /// </summary>
    public class StrongholdRiftManager : IRiftManager
    {
        private readonly IZone _zone;
        private readonly IZoneManager _zoneManager;
        private readonly IEntityServices _entityServices;
        private readonly IEnumerable<StrongholdRiftLocation> _spawnLocations;

        public StrongholdRiftManager(IZone zone, IEntityServices entityServices, ICustomRiftConfigReader customRiftConfigReader, IZoneManager zoneManager)
        {
            _zone = zone;
            _spawnLocations = StrongholdRiftLocationRepository.GetAllInZone(zone, customRiftConfigReader);
            _entityServices = entityServices;
            _zoneManager = zoneManager;
        }

        private bool _spawnedAll = false;
        private bool _spawning = false;
        public void Update(TimeSpan time)
        {
            if (_spawnedAll || _spawning)
                return;

            SpawnAll();
        }

        private void SpawnAll()
        {
            _spawning = true;
            foreach (var location in _spawnLocations)
            {
                SpawnRift(location);
            }
            _spawnedAll = true;
            _spawning = false;
        }

        private void SpawnRift(StrongholdRiftLocation spawn)
        {
            CustomRiftSpawner.TrySpawnRift(spawn.RiftConfig, _zoneManager, _zone.Id, spawn.Location, () =>
            {
                return (StrongholdExitRift)_entityServices.Factory.CreateWithRandomEID(DefinitionNames.STRONGHOLD_EXIT_RIFT);
            });
        }
    }
}
