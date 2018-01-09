using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionComponentsList : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionComponentsList(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_productionProcessor.GetComponentsList()).Send();
        }
    }
}