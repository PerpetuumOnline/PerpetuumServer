using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionInsuranceQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionInsuranceQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var targetEids = request.Data.GetOrDefault<long[]>(k.target);

            _productionManager.GetFacilityAndCheckDocking(facilityEid, character, out InsuraceFacility insuraceFacility);

            insuraceFacility.InsuranceQuery(character, targetEids, out Dictionary<string, object> result).ThrowIfError();

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}