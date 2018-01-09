using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public abstract class MarketStateRequestHandler : IRequestHandler
    {
        private readonly IMarketInfoService _marketInfoService;

        public MarketStateRequestHandler(IMarketInfoService marketInfoService)
        {
            _marketInfoService = marketInfoService;
        }

        public abstract void HandleRequest(IRequest request);

        protected Dictionary<string, object> GetMarketState()
        {
            return new Dictionary<string, object>
            {
                {k.marketCheckMargin, _marketInfoService.CheckAveragePrice},
                {"readableMargin", $"{_marketInfoService.Margin:f4}".Replace(',', '_')},
                {k.marketMargin, _marketInfoService.Margin},
            };
        }
    }
}