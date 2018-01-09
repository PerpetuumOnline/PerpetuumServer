using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionResearch : IRequestHandler
    {
        private readonly ProductionManager _productionManager;
        private readonly ProductionProcessor _productionProcessor;

        public ProductionResearch(ProductionManager productionManager,ProductionProcessor productionProcessor)
        {
            _productionManager = productionManager;
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var itemEid = request.Data.GetOrDefault<long>(k.item);
                var researchKitEid = request.Data.GetOrDefault<long>(k.researchKitEID);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;

                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out ResearchLab researchLab, out PublicContainer container);

                var replyDict = _productionProcessor.ResearchItem(researchLab, character, container, itemEid, researchKitEid, useCorporationWallet);
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}