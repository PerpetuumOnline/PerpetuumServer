using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneGetBuildings : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var result = request.Zone.GetBuildingsDictionaryForCharacter(character);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}