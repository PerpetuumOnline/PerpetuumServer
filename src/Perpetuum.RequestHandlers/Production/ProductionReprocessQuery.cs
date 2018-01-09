using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionReprocessQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionReprocessQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var targetEiDs = request.Data.GetOrDefault<long[]>(k.target);
            var character = request.Session.Character;

            //optional
            var sourceContainerEid = request.Data.GetOrDefault<long>(k.sourceContainer);
            _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out Reprocessor reprocessor, out PublicContainer sourceContainer);
            var replyDict = reprocessor.ReprocessQuery(character, sourceContainer, targetEiDs);
            Message.Builder.FromRequest(request).WithData(replyDict).Send();
        }
    }
}