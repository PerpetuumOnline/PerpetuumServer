using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);
            var forCorporation = request.Data.GetOrDefault<int>(k.corporation) == 1;

            PrivateCorporation corporation = null;

            if (forCorporation)
            {
                corporation = character.GetPrivateCorporationOrThrow();

                corporation.GetMemberRole(character).IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant, CorporationRole.ProductionManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
            }

            var dictionary = new Dictionary<string, object>
            {
                { k.history, ProductionHelper.ProductionLogList(character, offsetInDays, corporation) },
                { k.corporation, forCorporation }
            };

            Message.Builder.FromRequest(request)
                .WithData(dictionary)
                .WrapToResult()
                .Send();
        }
    }
}