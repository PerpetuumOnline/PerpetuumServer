using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionReprocess : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionReprocess(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var targetEiDs = request.Data.GetOrDefault<long[]>(k.target);
                var character = request.Session.Character;

                PublicContainer sourceContainer;
                Reprocessor reprocessor;
                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out reprocessor, out sourceContainer);

                var replyDict = _productionManager.ProductionProcessor.Reprocess(character, sourceContainer, targetEiDs, reprocessor);
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}