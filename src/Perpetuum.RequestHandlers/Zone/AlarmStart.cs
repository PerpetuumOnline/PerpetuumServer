using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class AlarmStart : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var eid = request.Data.GetOrDefault<long>(k.eid);

            var player = request.Zone.GetPlayerOrThrow(character);
            var alarmSwitch = request.Zone.GetUnitOrThrow<AlarmSwitch>(eid);

            alarmSwitch.Use(player);

            var result = alarmSwitch.GetUseResult();
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}