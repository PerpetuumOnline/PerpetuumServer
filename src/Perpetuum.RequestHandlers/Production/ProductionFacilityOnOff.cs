using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionFacilityOnOff : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionFacilityOnOff(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var facilityEID = request.Data.GetOrDefault<long>(k.facility);
            var facilityState = request.Data.GetOrDefault<int>(k.state) == 1;

            _productionProcessor.FacilityOnOff(facilityEID, facilityState);

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}