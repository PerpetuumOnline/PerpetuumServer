using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionInsuranceDelete : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionInsuranceDelete(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var targetEid = request.Data.GetOrDefault<long>(k.target);

            _productionProcessor.InsuranceDelete(character, targetEid).ThrowIfError();

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
        }
    }
}