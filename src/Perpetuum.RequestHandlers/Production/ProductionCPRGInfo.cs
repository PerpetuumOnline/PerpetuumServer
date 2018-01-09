using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionCPRGInfo : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionCPRGInfo(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var facility = request.Data.GetOrDefault<long>(k.facility);
            var cprgEid = request.Data.GetOrDefault<long>(k.eid);
            var character = request.Session.Character;

            _productionManager.PrepareProductionForPublicContainer(facility, character, out Mill mill, out PublicContainer container);
            var replyDict = ProductionProcessor.LineQuery(character, container, cprgEid, mill);
            Message.Builder.FromRequest(request).WithData(replyDict).Send();
        }
    }
}