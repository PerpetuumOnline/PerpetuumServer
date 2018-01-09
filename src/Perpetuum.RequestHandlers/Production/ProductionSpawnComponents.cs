using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionSpawnComponents : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionSpawnComponents(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var definition = request.Data.GetOrDefault<int>(k.definition);
            _productionProcessor.IsProducible(definition).ThrowIfFalse(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var description = _productionProcessor.GetProductionDescription(definition).ThrowIfNull(ErrorCodes.DefinitionNotSupported);
            description.SpawnRequiredComponentsAdmin(character);

            Message.Builder.FromRequest(request).WithData(new Dictionary<string, object>(1) { { k.result, k.oke } }).Send();
        }
    }
}