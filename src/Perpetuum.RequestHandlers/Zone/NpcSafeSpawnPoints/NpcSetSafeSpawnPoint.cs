using System.Drawing;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.NpcSystem.SafeSpawnPoints;

namespace Perpetuum.RequestHandlers.Zone.NpcSafeSpawnPoints
{
    public class NpcSetSafeSpawnPoint : NpcSafeSpawnPointRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var x = request.Data.GetOrDefault<int>(k.x);
                var y = request.Data.GetOrDefault<int>(k.y);

                var point = new SafeSpawnPoint
                {
                    Id = id,
                    Location = new Point(x, y)
                };

                request.Zone.SafeSpawnPoints.Update(point);
                SendSafeSpawnPoints(request);
                
                scope.Complete();
            }
        }
    }
}