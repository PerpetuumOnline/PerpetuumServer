using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionLineStart : IRequestHandler
    {
        private readonly ProductionManager _productionManager;
        private readonly ProductionProcessor _productionProcessor;

        public ProductionLineStart(ProductionManager productionManager,ProductionProcessor productionProcessor)
        {
            _productionManager = productionManager;
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var lineId = request.Data.GetOrDefault<int>(k.ID);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;
                var searchInRobots = request.Data.GetOrDefault<int>(k.inventory) != 1;
                var rounds = request.Data.GetOrDefault<int>(k.rounds);

                if (rounds < 0)
                    rounds = 1;

                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out Mill mill, out PublicContainer sourceContainer);

                var replyDict = _productionProcessor.LineStartInMill(character, sourceContainer, lineId, 1, useCorporationWallet, searchInRobots, mill, rounds);
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}