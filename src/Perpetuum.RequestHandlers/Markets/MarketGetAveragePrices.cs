using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketGetAveragePrices : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var marketEID = request.Data.GetOrDefault<long>(k.marketEID);
            var itemDefinition = request.Data.GetOrDefault<int>(k.definition);
            var day = request.Data.GetOrDefault<int>(k.day);

            var character = request.Session.Character;
            character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

            var market = Market.GetOrThrow(marketEID);
            var result = market.GetAverageHistory(day, itemDefinition);
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}