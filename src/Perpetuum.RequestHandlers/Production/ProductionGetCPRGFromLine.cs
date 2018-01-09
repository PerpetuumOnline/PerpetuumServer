using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionGetCPRGFromLine : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionGetCPRGFromLine(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var lineId = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);

                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out Mill mill, out PublicContainer publicContainer);

                var productionLine = ProductionLine.LoadByIdAndCharacterAndFacility(character, lineId, facilityEid);

                productionLine.IsActive().ThrowIfTrue(ErrorCodes.ProductionIsRunningOnThisLine);

                var calibrationProgram = productionLine.ExtractCalibrationProgram(mill);

                //add to public container
                publicContainer.AddItem(calibrationProgram, character.Eid, false);

                //save container
                publicContainer.Save();

                //delete line
                ProductionLine.DeleteById(productionLine.Id);

                ProductionHelper.ProductionLogInsert(character, productionLine.TargetDefinition, 1, ProductionInProgressType.removeCT, 0, 0, false);

                var linesList = mill.GetLinesList(character);
                var containerList = publicContainer.ToDictionary();

                var result = new Dictionary<string, object>
                {
                    {k.lineCount, linesList.Count},
                    {k.lines, linesList},
                    {k.container, containerList}
                };


                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}