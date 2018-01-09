using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionMergeResearchKitsMultiQuery : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionMergeResearchKitsMultiQuery(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var target = request.Data.GetOrDefault<long>(k.eid);
            var facilityEid = request.Data.GetOrDefault<long>(k.facility);
            var quantity = request.Data.GetOrDefault<int>(k.amount);
            var character = request.Session.Character;

            var pf = _productionManager.ProductionProcessor.GetFacility(facilityEid);

            var researchKitForge = pf as PBSResearchKitForgeFacility;

            researchKitForge.ThrowIfNull(ErrorCodes.FacilityTypeMismatch);

            PBSResearchKitForgeFacility pbsResearchKitForgeFacility;
            PublicContainer publicContainer;
            _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out pbsResearchKitForgeFacility, out  publicContainer);

            int nextLevel;
            int nextDefinition;
            double fullPrice;
            int availableQuantity;
            int searchDefinition;

            pbsResearchKitForgeFacility.PrepareResearchKitMerge(publicContainer, character, target,quantity, out nextDefinition, out nextLevel, out fullPrice, out availableQuantity, out searchDefinition).ThrowIfError();

            var result = new Dictionary<string, object>
            {
                {k.definition, nextDefinition},
                {k.level, nextLevel},
                {k.price, fullPrice},
                {k.amount, availableQuantity}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}