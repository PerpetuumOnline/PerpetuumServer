using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionLineList : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionLineList(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var character = request.Session.Character;

            _productionManager.GetFacilityAndCheckDocking(facilityEid, character, out Mill mill);

            var linesList = mill.GetLinesList(character);
            var facilityInfo = mill.GetFacilityInfo(character);

            var reply = new Dictionary<string, object>
            {
                {k.lineCount, linesList.Count},
                {k.lines, linesList},
                {k.facility, facilityInfo}
            };

            Message.Builder.FromRequest(request).WithData(reply).Send();
        }
    }
}