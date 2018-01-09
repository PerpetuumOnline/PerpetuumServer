using System;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketTaxLogList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var marketEid = request.Data.GetOrDefault<long>(k.marketEID);
            var market = Market.GetOrThrow(marketEid);

            var offsetInDay = request.Data.GetOrDefault<int>(k.offset);
            var logger = market.GetTaxChangeLogger();
            var history = logger.GetHistory(TimeSpan.FromDays(offsetInDay), TimeSpan.FromDays(2));

            var result = history.ToDictionary("a", e => e.ToDictionary());
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}