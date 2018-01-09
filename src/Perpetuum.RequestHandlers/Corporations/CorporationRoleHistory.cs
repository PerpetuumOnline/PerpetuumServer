using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationRoleHistory : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationRoleHistory(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);

            var character = request.Session.Character;
            Corporation corporation = character.GetPrivateCorporationOrThrow();
            corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.HRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

            var result = new Dictionary<string, object>
            {
                { k.corporationEID, corporation.Eid },
                { k.history,_corporationManager.GetCorporationRoleHistory(corporation.Eid, offsetInDays) }
            };

            Message.Builder.FromRequest(request)
                .WithData(result)
                .WrapToResult()
                .Send();
        }
    }
}