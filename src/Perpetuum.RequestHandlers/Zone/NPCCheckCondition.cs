using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.RequestHandlers.Zone
{
    public class NPCCheckCondition : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var safePoints = request.Zone.SafeSpawnPoints.GetAll();

            foreach (var safePoint in safePoints)
            {
                var checkConditionsInRange = request.Zone.IsTerrainConditionsMatchInRange(safePoint.Location.ToPosition(), 8, 4.0);
                if (!checkConditionsInRange)
                {
                    Logger.Error("consistency error! invalid roaming position: " + safePoint.Location + " zoneid: " + request.Zone.Id);
                }
            }

            foreach (var presence in request.Zone.PresenceManager.GetPresences())
            {
                var presenceType = presence.Configuration.PresenceType;
                if (presenceType != PresenceType.Normal && presenceType != PresenceType.Random) 
                    continue;

                foreach (var npcFlock in presence.Flocks)
                {
                    var checkConditionsInRange = request.Zone.IsTerrainConditionsMatchInRange(npcFlock.SpawnOrigin, npcFlock.Configuration.SpawnRange.Max, 4.0);
                    if (!checkConditionsInRange)
                    {
                        Logger.Error("consistency error! invalid roaming position: " + npcFlock.Configuration.SpawnOrigin.ToPoint() + " zoneid: " + request.Zone.Id + " presence:" + presence.Configuration.Name + " pres ID:" + presence.Configuration.ID + " " + npcFlock.Configuration.Name + " npcflockID:" + npcFlock.Configuration.ID);
                    }
                }
            }
        }
    }
}