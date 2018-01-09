using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneNpcFlockKill : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var presenceId = request.Data.GetOrDefault<int>(k.presenceID);
            var flockId = request.Data.GetOrDefault<int>(k.flockID);

            var presence = request.Zone.PresenceManager.GetPresences().GetPresenceOrThrow(presenceId);
            var flock = presence.Flocks.GetFlockOrThrow(flockId);
            flock.Members.KillAll();
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}