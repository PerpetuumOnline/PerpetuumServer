using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    //csak bedokkolva, public container rootbol vasarol ID alapjan 
    //opcionalis quantity
    public class ItemShopBuy : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var locationEid = request.Data.GetOrDefault<long>(k.eid);
                var entryId = request.Data.GetOrDefault<int>(k.ID);
                var quantity = request.Data.GetOrDefault(k.quantity, 1);

                quantity = quantity.Clamp(1, 500000);
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var dockingBase = character.GetCurrentDockingBase();
                if (dockingBase == null)
                    throw new PerpetuumException(ErrorCodes.DockingBaseNotFound);

                var shop = dockingBase.GetItemShop();
                if (shop == null || shop.Eid != locationEid)
                    throw new PerpetuumException(ErrorCodes.ItemNotFound);

                var publicContainer = character.GetPublicContainerWithItems();
                shop.Buy(publicContainer,character, entryId, quantity);

                publicContainer.Save();

                var result = publicContainer.ToDictionary();
                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> {{k.container, result}}).Send();
                
                scope.Complete();
            }
        }
    }
}