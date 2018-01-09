using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionInsuranceBuy : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionInsuranceBuy(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var targetEids = request.Data.GetOrDefault<long[]>(k.target);

                _productionManager.GetFacilityAndCheckDocking(facilityEid, character, out InsuraceFacility insuraceFacility);

                insuraceFacility.InsuranceBuy(character, targetEids);

                var list = InsuranceHelper.InsuranceList(character);
                var result = new Dictionary<string, object>
                {
                    {k.insurance, list}
                };

                if (list.Count > 0)
                {
                    Message.Builder.FromRequest(request).WithData(result).Send();
                }
                else
                {
                    Message.Builder.FromRequest(request).WithEmpty().Send();
                }
                
                scope.Complete();
            }
        }
    }
}