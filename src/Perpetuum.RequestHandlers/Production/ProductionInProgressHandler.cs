using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionInProgressHandler : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionInProgressHandler(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var result = new Dictionary<string, object>
            {
                {"personal",_productionProcessor.RunningProductions.GetByCharacter(character).ToDictionary()}
            };

            var corporation = character.GetPrivateCorporation();
            if (corporation != null)
            {
                var memberRole = corporation.GetMemberRole(character);

                if (memberRole.IsAnyRole(CorporationRole.ProductionManager, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant))
                {
                    result.Add("corporation",_productionProcessor.RunningProductions.GetByCorporation(corporation).ToDictionary());
                }
            }

            if (result.Count > 0)
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