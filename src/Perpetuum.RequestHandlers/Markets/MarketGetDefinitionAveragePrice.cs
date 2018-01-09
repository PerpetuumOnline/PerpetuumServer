using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketGetDefinitionAveragePrice : IRequestHandler
    {
        private readonly IEntityServices _entityServices;
        private readonly IMarketInfoService _marketInfoService;
        private readonly MarketHandler _marketHandler;

        public MarketGetDefinitionAveragePrice(IEntityServices entityServices,IMarketInfoService marketInfoService,MarketHandler marketHandler)
        {
            _entityServices = entityServices;
            _marketInfoService = marketInfoService;
            _marketHandler = marketHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var definition = request.Data.GetOrDefault<int>(k.definition);
            var character = request.Session.Character;
            var marketEid = request.Data.GetOrDefault<long>(k.eid);

            if (!_entityServices.Defaults.TryGet(definition, out EntityDefault ed))
                throw new PerpetuumException(ErrorCodes.DefinitionNotSupported);

            if (!ed.Purchasable)
                throw new PerpetuumException(ErrorCodes.ItemNotPurchasable);

            var market = marketEid == 0 ? character.GetCurrentDockingBase().GetMarketOrThrow() : Market.GetOrThrow(marketEid);

            var tax = market.GetMarketTaxRate(character);
            var feeRate = Market.GetMarketFeeRate(character);
            var feeReal = feeRate * _marketInfoService.Fee;

            var avgPrice = _marketHandler.GetAveragePriceByMarket(market, definition).AveragePrice;

            var result = new Dictionary<string, object>
            {
                {k.average, avgPrice},
                {k.definition, definition},
                {k.tax, 1.0 - tax},
                {k.feeRate, feeRate},
                {k.fee, feeReal},
                {k.marketMargin,_marketInfoService.Margin},
                {k.marketCheckMargin, _marketInfoService.CheckAveragePrice}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}