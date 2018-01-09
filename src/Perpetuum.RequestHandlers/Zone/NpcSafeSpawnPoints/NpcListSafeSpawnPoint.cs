using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone.NpcSafeSpawnPoints
{
    public class NpcListSafeSpawnPoint : NpcSafeSpawnPointRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            SendSafeSpawnPoints(request);
        }
    }
}