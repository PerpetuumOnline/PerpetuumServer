using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    //raw material -> basic commodity QUERY
    public class ProductionRefineQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionRefineQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var definition = request.Data.GetOrDefault<int>(k.definition);
            var amount = request.Data.GetOrDefault<int>(k.amount);
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var character = request.Session.Character;

            amount = amount.Clamp(0, 1000000);

            _productionManager.ProductionProcessor.CheckTargetDefinitionAndThrowIfFailed(definition);

            var refinery = _productionManager.GetFacility<Refinery>(facilityEid);
            refinery.IsOpen.ThrowIfFalse(ErrorCodes.FacilityClosed);

            var replyDict = _productionManager.ProductionProcessor.RefineQuery(refinery, character, definition, amount);
            Message.Builder.FromRequest(request).WithData(replyDict).Send();
        }
    }
}