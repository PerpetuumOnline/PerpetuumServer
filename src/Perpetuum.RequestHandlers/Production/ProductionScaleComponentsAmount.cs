using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionScaleComponentsAmount : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionScaleComponentsAmount(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var targetCategoryFlag = (CategoryFlags)request.Data.GetOrDefault<long>(k.targets);
                var componentCategory = (CategoryFlags)request.Data.GetOrDefault<long>(k.materials);
                var scale = request.Data.GetOrDefault<double>(k.ratio);

                (!targetCategoryFlag.IsCategoryExists() || !componentCategory.IsCategoryExists()).ThrowIfTrue(ErrorCodes.ServerError);

                _productionProcessor.ScaleComponentsAmount(scale, targetCategoryFlag, componentCategory);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}