using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketCreateGammaPlasmaOrders : IRequestHandler
    {
        private readonly MarketHelper _marketHelper;

        public MarketCreateGammaPlasmaOrders(MarketHelper marketHelper)
        {
            _marketHelper = marketHelper;
        }

        public void HandleRequest(IRequest request)
        {
            _marketHelper.CreatePlasmaBuyOrdersOnExistingGammaBases();
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}