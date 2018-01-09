using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    //raw material -> basic commodity
    public class ProductionRefine : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionRefine(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var definition = request.Data.GetOrDefault<int>(k.definition);
                var amount = request.Data.GetOrDefault<int>(k.amount);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var character = request.Session.Character;
                var searchInRobots = request.Data.GetOrDefault<int>(k.inventory) != 1;

                amount = amount.Clamp(0, 1000000);

                _productionManager.ProductionProcessor.CheckTargetDefinitionAndThrowIfFailed(definition);

                Refinery refinery;
                PublicContainer sourceContainer;
                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out refinery, out sourceContainer);

                var replyDict = _productionManager.ProductionProcessor.Refine(refinery, character, sourceContainer, definition, amount);
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}