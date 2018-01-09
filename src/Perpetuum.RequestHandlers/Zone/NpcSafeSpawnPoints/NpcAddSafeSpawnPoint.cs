using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.NpcSafeSpawnPoints
{
    public class NpcAddSafeSpawnPoint : NpcSafeSpawnPointRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);
                AddSafeSpawnPoint(request, player.CurrentPosition);
               
                scope.Complete();
            }
        }
    }
}