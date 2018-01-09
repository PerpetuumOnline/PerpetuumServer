using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionCancel : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionCancel(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;
                _productionProcessor.CancelProduction(character, id);
                
                scope.Complete();
            }
        }
    }
}