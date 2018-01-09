using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionRemoveFacility : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionRemoveFacility(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var facilityEID = request.Data.GetOrDefault<long>(k.eid);

                var facility = _productionProcessor.GetFacility(facilityEID);
                if (facility != null)
                    _productionProcessor.RemoveFacility(facility);

                var replyDict = new Dictionary<string, object>(1) { { k.result, facilityEID } };
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}