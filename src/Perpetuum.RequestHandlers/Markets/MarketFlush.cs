using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketFlush : IRequestHandler
    {
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketFlush(IMarketOrderRepository marketOrderRepository)
        {
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var marketEID = request.Data.GetOrDefault<long>(k.market);

                var market = Market.GetOrThrow(marketEID);

                var marketOrders = _marketOrderRepository.GetByMarket(market);

                var counter = 0;
                foreach (var order in marketOrders)
                {
                    order.Cancel(_marketOrderRepository);
                    counter++;
                }

                var orphanCount = market.GetItemsCount(); //items without market order (market item)

                var result = new Dictionary<string, object>
                {
                    {k.items, counter},
                    {k.orphan, orphanCount}
                };

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}