using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionGetCPRGFromLineQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionGetCPRGFromLineQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var lineId = request.Data.GetOrDefault<int>(k.ID);
            var character = request.Session.Character;
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);

            _productionManager.GetFacilityAndCheckDocking(facilityEid, character, out Mill mill);

            var productionLine = ProductionLine.LoadByIdAndCharacterAndFacility(character, lineId, facilityEid);

            productionLine.CalculateDecalibrationPenalty(character, out int materialEfficiency, out int timeEfficiency);

            var calibrationProgramDefinition = productionLine.GetCalibrationTemplateDefinition();

            var result = new Dictionary<string, object>
            {
                {k.materialEfficiency, materialEfficiency},
                {k.timeEfficiency, timeEfficiency},
                {k.definition, calibrationProgramDefinition}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}