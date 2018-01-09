using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ItemShop;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneItemShopList : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var locationEid = request.Data.GetOrDefault<long>(k.eid);

            var character = request.Session.Character;
            var player = request.Zone.GetPlayer(character);
            if (player == null)
                throw new PerpetuumException(ErrorCodes.PlayerNotFound);

            var unit = request.Zone.GetUnitOrThrow<ItemShop>(locationEid);
            unit.IsInOperationRange(player).ThrowIfError();

            var result = unit.EntriesToDictionary();
            Message.Builder.FromRequest(request)
                           .WithData(new Dictionary<string, object> { { k.shop, result } })
                           .Send();
        }
    }
}