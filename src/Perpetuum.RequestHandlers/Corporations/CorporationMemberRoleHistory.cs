using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationMemberRoleHistory : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationMemberRoleHistory(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            var member = Character.Get(request.Data.GetOrDefault<int>(k.memberID));
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);

            var character = request.Session.Character;
            Corporation corporation = character.GetPrivateCorporationOrThrow();
            corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.HRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
            corporation.IsMember(member).ThrowIfFalse(ErrorCodes.CharacterNotCorporationMember);

            var result = new Dictionary<string, object>
            {
                { k.corporationEID, corporation.Eid },
                { k.history,_corporationManager.GetCorporationRoleHistoryOneCharacter(corporation.Eid, member, offsetInDays) }
            };
            Message.Builder.FromRequest(request)
                .WithData(result)
                .WrapToResult()
                .Send();
        }
    }
}