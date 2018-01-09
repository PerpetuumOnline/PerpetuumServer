using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionLineCalibrate : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionLineCalibrate(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var calibrationEid = request.Data.GetOrDefault<long>(k.eid);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var character = request.Session.Character;

                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out Mill mill, out PublicContainer container);

                var replyDict = mill.CalibrateLine(character, calibrationEid, container);
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}