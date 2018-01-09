using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketInsertStats : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var days = request.Data.GetOrDefault<int>(k.day);
                var definition = request.Data.GetOrDefault<int>(k.definition);
                var price = request.Data.GetOrDefault<double>(k.price);
                var amount = request.Data.GetOrDefault<int>(k.amount);
                var marketEid = request.Data.GetOrDefault<long>(k.marketEID);

                var market = Market.GetOrThrow(marketEid);

                market.InsertStatsForPeriod(days, price, amount, definition);

                var result = market.GetAverageHistory(days, definition);

                Message.Builder.SetCommand(Commands.MarketGetAveragePrices)
                    .WithData(result)
                    .ToClient(request.Session)
                    .Send();
                
                scope.Complete();
            }
        }
    }
}