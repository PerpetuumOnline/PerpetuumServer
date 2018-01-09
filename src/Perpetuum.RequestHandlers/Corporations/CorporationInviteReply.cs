using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationInviteReply : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationInviteReply(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var answer = request.Data.GetOrDefault<int>(k.answer);

                _corporationManager.Invites.TryGetInvite(character, out CorporationInviteInfo inviteInfo).ThrowIfFalse(ErrorCodes.NoSuchInvite);

                var corporation = inviteInfo.sender.GetPrivateCorporationOrThrow();

                //if accepted
                if (answer > 0)
                {
                    corporation.AddRecruitedMember(character, inviteInfo.sender);
                }

                _corporationManager.Invites.RemoveInvite(character);

                Message.Builder.SetCommand(Commands.CorporationInviteReply)
                    .WithData(new Dictionary<string, object>
                    {
                        { k.characterID, character},
                        { k.answer, answer }
                    })
                    .ToCharacter(inviteInfo.sender)
                    .Send();

                var replyDict = new Dictionary<string, object>
                {
                    {k.answer, answer},
                    {k.corporationEID,corporation.Eid}
                };

                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}