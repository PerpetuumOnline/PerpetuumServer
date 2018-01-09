using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionCPRGForgeQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionCPRGForgeQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var sourceEid = request.Data.GetOrDefault<long>(k.source);
            var targetEid = request.Data.GetOrDefault<long>(k.target);
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);

            var character = request.Session.Character;

            PBSCalibrationProgramForgeFacility calibrationProgramForgeFacility;
            PublicContainer publicContainer;
            _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out calibrationProgramForgeFacility, out publicContainer );

            var result = _productionManager.ProductionProcessor.QueryCPRGForge(character, sourceEid, targetEid, publicContainer, calibrationProgramForgeFacility);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}