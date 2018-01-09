using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.NpcSystem.SafeSpawnPoints;

namespace Perpetuum.RequestHandlers.Zone.NpcSafeSpawnPoints
{
    public class NpcDeleteSafeSpawnPoint : NpcSafeSpawnPointRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                request.Zone.SafeSpawnPoints.Delete(new SafeSpawnPoint { Id = id });
                SendSafeSpawnPoints(request);
                scope.Complete();
            }
        }
    }
}