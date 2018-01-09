using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.MissionRequests
{
    public class MissionGetSupply : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var structureEid = request.Data.GetOrDefault<long>(k.eid);

            var character = request.Session.Character;
            var player = request.Zone.GetPlayerOrThrow(character);
            var itemSupply = request.Zone.GetUnitOrThrow<ItemSupply>(structureEid);

            itemSupply.Use(player);
            var result = itemSupply.GetUseResult();
            Message.Builder.SetCommand(Commands.AlarmStart).WithData(result).ToClient(request.Session).Send();
        }
    }
}
