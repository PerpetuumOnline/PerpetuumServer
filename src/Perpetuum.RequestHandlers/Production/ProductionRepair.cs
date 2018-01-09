using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionRepair : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionRepair(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var target = request.Data.GetOrDefault<long[]>(k.target);
                var character = request.Session.Character;
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;

                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out Repair repairFacility, out PublicContainer sourceContainer);

                repairFacility.RepairItems(character, target, sourceContainer, useCorporationWallet);

                var result = new Dictionary<string, object> { { k.container, sourceContainer.ToDictionary() } };
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}