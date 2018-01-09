using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionPrototypeQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionPrototypeQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var targetDefinition = request.Data.GetOrDefault<int>(k.definition);
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);

            _productionManager.GetFacilityAndCheckDocking(facilityEid, character, out Prototyper prototyper);

            _productionManager.ProductionProcessor.PrototypeQuery(character, targetDefinition, prototyper, out Dictionary<string, object> replyDict).ThrowIfError();

            Message.Builder.FromRequest(request).WithData(replyDict).Send();
        }
    }
}