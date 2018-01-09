using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionCPRGForge : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionCPRGForge(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.source);
                var targetEid = request.Data.GetOrDefault<long>(k.target);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var character = request.Session.Character;
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;

                PBSCalibrationProgramForgeFacility calibrationProgramForgeFacility;
                PublicContainer publicContainer;
                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out calibrationProgramForgeFacility, out publicContainer);

                var result = _productionManager.ProductionProcessor.StartCalibrationProgramForge(character, sourceEid, targetEid, publicContainer, calibrationProgramForgeFacility, useCorporationWallet);
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}