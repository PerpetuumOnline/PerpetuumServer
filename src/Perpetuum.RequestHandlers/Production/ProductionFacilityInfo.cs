using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionFacilityInfo : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionFacilityInfo(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var targets = request.Data.GetOrDefault<long[]>(k.target);
            var baseEid = request.Data.GetOrDefault<long>(k.baseEID);
            var character = request.Session.Character;

            var queriedFacilities = baseEid > 0 ? _productionProcessor.GetFacilitiesByBaseEid(baseEid) : _productionProcessor.GetFacilitiesByEid(targets);
            var facilities = queriedFacilities.ToDictionary("f", f => f.GetFacilityInfo(character));

            var result = new Dictionary<string, object>
            {
                {k.facility, facilities}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}