using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketGetMyItems : IRequestHandler
    {
        private readonly MarketHelper _marketHelper;

        public MarketGetMyItems(MarketHelper marketHelper)
        {
            _marketHelper = marketHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var reply = _marketHelper.GetMarketOrdersInfo(character);
            Message.Builder.FromRequest(request).WithData(reply).WrapToResult().Send();
        }
    }
}