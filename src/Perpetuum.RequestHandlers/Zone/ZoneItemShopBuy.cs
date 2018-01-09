using System.Collections.Generic;
using System.Diagnostics;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ItemShop;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneItemShopBuy : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var locationEid = request.Data.GetOrDefault<long>(k.eid);
                var entryId = request.Data.GetOrDefault<int>(k.ID);
                var quantity = request.Data.GetOrDefault(k.quantity, 1);

                quantity = quantity.Clamp(1, 500000);

                var player = request.Zone.GetPlayerOrThrow(character);
                var shop = (ItemShop)request.Zone.GetUnit(locationEid).ThrowIfNull(ErrorCodes.ItemNotFound);
                shop.IsInOperationRange(player).ThrowIfError();

                var container = player.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();

                shop.Buy(container, character, entryId, quantity);

                container.Save();

                Transaction.Current.OnCompleted(c =>
                {
                    var result = container.ToDictionary();
                    Message.Builder.FromRequest(request).WithData(new Dictionary<string, object>
                    {
                        {k.container, result},
                    }).Send();
                });
                
                scope.Complete();
            }
        }
    }
}