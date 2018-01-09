using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Gangs
{
    public class GangInvite : IRequestHandler
    {
        private readonly IGangManager _gangManager;
        private readonly IGangInviteService _gangInviteService;

        public GangInvite(IGangManager gangManager,IGangInviteService gangInviteService)
        {
            _gangManager = gangManager;
            _gangInviteService = gangInviteService;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var member = Character.Get(request.Data.GetOrDefault<int>(k.memberID));

            var currentGang = _gangManager.GetGangByMember(member);
            if (currentGang != null)
                throw new PerpetuumException(ErrorCodes.CharacterAlreadyInGang);

            var gang = _gangManager.GetGangByMember(character);
            if (gang == null)
                throw new PerpetuumException(ErrorCodes.CharacterNotInGang);

            gang.CanInvite(character).ThrowIfFalse(ErrorCodes.OnlyGangLeaderOrAssistantCanDoThis);

            _gangInviteService.GetInvite(member).ThrowIfNotNull(ErrorCodes.CharacterAlreadyHasPendingGangInvitation);
            _gangInviteService.AddInvite(new GangInviteInfo(gang.Id, character, member));

            var data = new Dictionary<string, object>
            {
                { k.name, gang.Name }, { k.characterID, character.Id }
            };

            Message.Builder.SetCommand(Commands.GangInvite).WithData(data).ToCharacter(member).Send();
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}