using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionResearchQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionResearchQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var researchKitDefinition = request.Data.GetOrDefault<int>(k.definition);
            var targetDefinition = request.Data.GetOrDefault<int>(k.target);
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var character = request.Session.Character;

            var researchLab = _productionManager.GetFacility<ResearchLab>(facilityEid);
            researchLab.IsOpen.ThrowIfFalse(ErrorCodes.FacilityClosed);

            var replyDict = researchLab.ResearchQuery(character, researchKitDefinition, targetDefinition);
            Message.Builder.FromRequest(request).WithData(replyDict).Send();
        }
    }
}