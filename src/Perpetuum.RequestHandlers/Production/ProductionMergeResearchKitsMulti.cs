using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionMergeResearchKitsMulti : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionMergeResearchKitsMulti(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var target = request.Data.GetOrDefault<long>(k.eid);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var quantity = request.Data.GetOrDefault<int>(k.amount);
                var character = request.Session.Character;
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;

                _productionManager.ProductionProcessor.GetFacility(facilityEid).ThrowIfNotType<PBSResearchKitForgeFacility>(ErrorCodes.FacilityTypeMismatch);

                PBSResearchKitForgeFacility pbsResearchKitForgeFacility;
                PublicContainer publicContainer;
                _productionManager.PrepareProductionForPublicContainer(facilityEid, character, out pbsResearchKitForgeFacility, out  publicContainer);

                int nextLevel;
                int nextDefinition;
                double fullPrice;
                int availableQuantity;
                int searchDefinition;

                pbsResearchKitForgeFacility.PrepareResearchKitMerge(publicContainer, character, target, quantity, out nextDefinition, out nextLevel, out fullPrice, out availableQuantity, out searchDefinition).ThrowIfError();

                availableQuantity.ThrowIfEqual(0,ErrorCodes.MoreThanOneItemRequired);
            
                Dictionary<string, object> result;
                pbsResearchKitForgeFacility.DoResearchKitMerge(publicContainer, character, nextDefinition, nextLevel, fullPrice, availableQuantity, searchDefinition, out result, useCorporationWallet).ThrowIfError();

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}