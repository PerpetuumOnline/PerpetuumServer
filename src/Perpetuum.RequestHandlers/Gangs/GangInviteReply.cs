using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Gangs
{
    public class GangInviteReply : IRequestHandler
    {
        private readonly IGangManager _gangManager;
        private readonly IGangInviteService _gangInviteService;

        public GangInviteReply(IGangManager gangManager,IGangInviteService gangInviteService)
        {
            _gangManager = gangManager;
            _gangInviteService = gangInviteService;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var answer = request.Data.GetOrDefault<int>(k.answer);

                var currentGang = _gangManager.GetGangByMember(character);
                if (currentGang != null)
                    throw new PerpetuumException(ErrorCodes.CharacterAlreadyInGang);

                var gangInvite = _gangInviteService.GetInvite(character).ThrowIfNull(ErrorCodes.NoSuchGangInvite);
                var gang = _gangManager.GetGang(gangInvite.gangGuid).ThrowIfNull(ErrorCodes.GangNotFound);

                _gangInviteService.RemoveInvite(gangInvite);

                if (answer > 0)
                {
                    _gangManager.JoinMember(gang, character, true);
                }

                Transaction.Current.OnCommited(() =>
                {
                    var data = new Dictionary<string, object>
                    {
                        { k.characterID, gangInvite.member.Id },
                        { k.answer, answer }
                    };

                    // feladonak
                    Message.Builder.SetCommand(Commands.GangInviteReply)
                        .WithData(data)
                        .ToCharacter(gangInvite.sender)
                        .Send();

                    data = new Dictionary<string, object> { { k.answer, answer } };

                    if (answer > 0)
                    {
                        data.Add(k.gang, gang.ToDictionary());
                    }

                    // membernek
                    Message.Builder.FromRequest(request).WithData(data).Send();
                });
                
                scope.Complete();
            }
        }
    }
}