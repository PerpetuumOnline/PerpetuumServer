using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketGetState : MarketStateRequestHandler
    {
        public MarketGetState(IMarketInfoService marketInfoService) : base(marketInfoService)
        {
        }

        public override void HandleRequest(IRequest request)
        {
            var result = GetMarketState();
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}