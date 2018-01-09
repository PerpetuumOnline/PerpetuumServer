using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionForceEnd : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionForceEnd(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            _productionProcessor.ForceEndAllProduction();
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}