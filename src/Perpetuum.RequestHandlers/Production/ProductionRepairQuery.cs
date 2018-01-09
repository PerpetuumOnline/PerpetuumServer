using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionRepairQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionRepairQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var target = request.Data.GetOrDefault<long[]>(k.target);
            var character = request.Session.Character;

            _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out Repair repairFacility, out PublicContainer sourceContainer);

            var result = repairFacility.QueryPrices(character, sourceContainer, target);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}