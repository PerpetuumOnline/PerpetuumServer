using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketCancelItem : IRequestHandler
    {
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketCancelItem(IMarketOrderRepository marketOrderRepository)
        {
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var marketItemID = request.Data.GetOrDefault<int>(k.marketItemID);

                var result = new Dictionary<string, object>();

                var marketOrder = _marketOrderRepository.Get(marketItemID).ThrowIfNull(ErrorCodes.ItemNotFound);

                var character = request.Session.Character;
                marketOrder.submitterEID.ThrowIfNotEqual(character.Eid, ErrorCodes.OwnerMismatch);

                //check minimal duration
                if (!marketOrder.IsModifyTimeValid())
                {
                    Message.Builder.FromRequest(request).WithData(marketOrder.GetValidModifyInfo()).WrapToResult().Send();
                    return;
                }

                var canceledItem = marketOrder.Cancel(_marketOrderRepository);

                if (canceledItem != null)
                    result.Add(k.item, canceledItem.BaseInfoToDictionary());

                result.Add(k.marketItemID, marketOrder.id); //return the item id anyways

                Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                
                scope.Complete();
            }
        }
    }
}