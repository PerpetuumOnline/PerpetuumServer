using Perpetuum.Host.Requests;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneNpcFlockSetParameter : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var presenceId = request.Data.GetOrDefault<int>(k.presenceID);
            var flockId = request.Data.GetOrDefault<int>(k.ID);

            //input parameters ... softly

            var presence = request.Zone.PresenceManager.GetPresences().GetPresenceOrThrow(presenceId);
            var flock = presence.Flocks.GetFlockOrThrow(flockId);
            if (flock is NormalFlock normalFlock)
            {
                var respawnMultiplierLow = request.Data.GetOrDefault(k.respawnMultiplier, 0.75);
                normalFlock.respawnMultiplierLow = respawnMultiplierLow;
                normalFlock.respawnMultiplier = respawnMultiplierLow;
            }

            var result = request.Zone.PresenceManager.GetPresences().ToDictionary("p", p => p.ToDictionary(true));
            Message.Builder.SetCommand(Commands.ZoneListPresences)
                .WithData(result)
                .ToClient(request.Session)
                .Send();
        }
    }
}