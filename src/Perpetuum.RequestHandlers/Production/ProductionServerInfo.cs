using System.Collections.Generic;
using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionServerInfo : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionServerInfo(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var result = new Dictionary<string, object>
            {
                {"runningProductionAmount",_productionProcessor.RunningProductions.Count()},
                //amit meg akarunk...
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}