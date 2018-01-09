using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionLineDelete : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionLineDelete(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var lineId = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;

                _productionManager.GetFacilityAndCheckDocking(facilityEid, character, out Mill mill);

                var replyDict = mill.DeleteLine(character, lineId);
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}