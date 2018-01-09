using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionPrototypeStart : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionPrototypeStart(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var targetDefinition = request.Data.GetOrDefault<int>(k.definition);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;
                var searchInRobots = request.Data.GetOrDefault<int>(k.inventory) > 0;

                character.TechTreeNodeUnlocked(targetDefinition).ThrowIfFalse(ErrorCodes.TechTreeNodeNotFound);

                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out Prototyper prototyper, out PublicContainer container);

                var replyDict = _productionManager.ProductionProcessor.PrototypeStart(character, targetDefinition, container, prototyper, useCorporationWallet);
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}