using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketGlobalAveragePrices : IRequestHandler
    {
        private readonly MarketHandler _marketHandler;

        public MarketGlobalAveragePrices(MarketHandler marketHandler)
        {
            _marketHandler = marketHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var itemDefinition = request.Data.GetOrDefault<int>(k.definition);
            var day = request.Data.GetOrDefault<int>(k.day);

            var result = _marketHandler.GetGlobalAverageHistory(day, itemDefinition);
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}