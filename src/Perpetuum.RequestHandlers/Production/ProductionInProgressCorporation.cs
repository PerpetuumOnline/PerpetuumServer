using System.Collections.Generic;
using System.Linq;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionInProgressCorporation : IRequestHandler
    {
        private readonly ProductionProcessor _productionProcessor;

        public ProductionInProgressCorporation(ProductionProcessor productionProcessor)
        {
            _productionProcessor = productionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var replyDict = new Dictionary<string, object>();

            var corporation = character.GetPrivateCorporationOrThrow();

            corporation.GetMemberRole(character)
                .IsAnyRole(CorporationRole.ProductionManager, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant).ThrowIfFalse(ErrorCodes.AccessDenied);

            var members = corporation.Members.Select(m => m.character).ToArray();

            var counter = 0;
            var runningProductions = _productionProcessor.RunningProductions.ToArray();

            if (request.Data.TryGetValue(k.facility, out long facilityEID))
            {
                foreach (var member in members)
                {
                    var oneResult = new Dictionary<string, object>
                    {
                        {k.production, ProductionInProgressExtensions.GetCorporationPaidProductionsByFacililtyAndCharacter(runningProductions, member, facilityEID).ToDictionary("c", p => p.ToDictionary())},
                        {k.characterID, member.Id}
                    };

                    replyDict.Add("c" + counter++, oneResult);
                }
            }
            else
            {
                foreach (var member in members)
                {
                    var oneResult = new Dictionary<string, object>
                    {
                        {k.production, ProductionInProgressExtensions.GetCorporationPaidProductionsByCharacter(runningProductions, member).ToDictionary("c", p => p.ToDictionary())},
                        {k.characterID, member.Id}
                    };

                    replyDict.Add("c" + counter++, oneResult);
                }
            }

            if (replyDict.Count > 0)
            {
                Message.Builder.FromRequest(request).WithData(replyDict).Send();
            }
            else
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
            }
        }
    }
}