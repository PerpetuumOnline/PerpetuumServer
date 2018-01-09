using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketListFacilities : IRequestHandler
    {
        private readonly MarketHelper _marketHelper;

        public MarketListFacilities(MarketHelper marketHelper)
        {
            _marketHelper = marketHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var result = _marketHelper.GetDefaultMarketsToDictionary;
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}