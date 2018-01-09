using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class TriggerMissionStructure : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var eid = request.Data.GetOrDefault<long>(k.eid);

            var player = request.Zone.GetPlayerOrThrow(character);
            var missionStructure = request.Zone.GetUnitOrThrow<MissionStructure>(eid);
            missionStructure.IsInRangeOf3D(player, DistanceConstants.KIOSK_USE_DISTANCE).ThrowIfFalse(ErrorCodes.ItemOutOfRange);
            missionStructure.CreateSuccessBeam(player);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}