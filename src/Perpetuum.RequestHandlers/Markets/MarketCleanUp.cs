using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketCleanUp : IRequestHandler
    {
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketCleanUp(IMarketOrderRepository marketOrderRepository)
        {
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var orderz = _marketOrderRepository.GetOrdersToCleanup().ToList();

            Logger.Info(orderz.Count + " market orders to clean up. ");

            foreach (var marketOrder in orderz)
            {
                marketOrder.Cancel(_marketOrderRepository);
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }

}