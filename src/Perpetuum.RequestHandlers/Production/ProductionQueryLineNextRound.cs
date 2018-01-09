using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionQueryLineNextRound : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionQueryLineNextRound(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var lineId = request.Data.GetOrDefault<int>(k.ID);
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var character = request.Session.Character;

            var processor = _productionManager.ProductionProcessor;

            var mill = processor.GetFacility(facilityEid).ThrowIfNotType<Mill>(ErrorCodes.DefinitionNotSupported);

            ProductionLine productionLine;
            ProductionLine.LoadById(lineId, out productionLine).ThrowIfError();

            productionLine.CharacterId.ThrowIfNotEqual(character.Id, ErrorCodes.AccessDenied);

            //query production result for the next round
            var lineResult = mill.QueryMaterialAndTime(productionLine.GetOrCreateCalibrationProgram(mill),  character, productionLine.TargetDefinition, productionLine.GetMaterialPoints(), productionLine.GetTimePoints(), true);

            //do fake decalibration
            var newMaterialEfficiency = productionLine.MaterialEfficiency;
            var newTimeEfficiency = productionLine.TimeEfficiency;

            Logger.Info("pre decalibration mateff:" + newMaterialEfficiency + " timeeff:" + newTimeEfficiency);

            productionLine.GetDecalibratedEfficiencies(ref newMaterialEfficiency, ref newTimeEfficiency);

            Logger.Info("post decalibration mateff:" + newMaterialEfficiency + " timeeff:" + newTimeEfficiency);

            productionLine.MaterialEfficiency = newMaterialEfficiency;
            productionLine.TimeEfficiency = newTimeEfficiency;


            var oneLine = new Dictionary<string, object>
            {
                {k.line, lineResult},
                {k.data, productionLine.ToDictionary()}
            };

            var result = new Dictionary<string, object>
            {
                {"l1", oneLine}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }

    }
}