using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.RequestHandlers.Zone
{
    public class TeleportQueryWorldChannels : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var capsuleEid = request.Data.GetOrDefault<long>(k.eid);

            var character = request.Session.Character;
            var player = request.Zone.GetPlayerOrThrow(character);

            var teleportDeployer = (MobileTeleportDeployer) player.GetContainer().GetItemOrThrow(capsuleEid);
            var teleports = teleportDeployer.WorldTargetHelper.GetWorldTargets(request.Zone, player.CurrentPosition, 0, (int)DistanceConstants.MOBILE_TELEPORT_USE_RANGE, teleportDeployer.WorkingRange);

            var result = new Dictionary<string, object>
            {
                {k.channels, teleports.ToDictionary("t", d => d.ToDictionary())}
            };

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}
