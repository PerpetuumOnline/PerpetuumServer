using System.Collections.Generic;

namespace Perpetuum.Zones.NpcSystem.SafeSpawnPoints
{
    public interface ISafeSpawnPointsRepository
    {
        void Add(SafeSpawnPoint point);
        void Update(SafeSpawnPoint point);
        void Delete(SafeSpawnPoint point);
        IEnumerable<SafeSpawnPoint> GetAll();
    }
}