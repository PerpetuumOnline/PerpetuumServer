using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionFacilityDescription : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionFacilityDescription(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var infos = _productionProcessor.Facilities.ToDictionary("i", f => f.BaseInfoToDictionary());
            Message.Builder.FromRequest(request)
                .WithData(infos)
                .Send();
        }
    }
}