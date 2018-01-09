using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Social;

namespace Perpetuum.RequestHandlers.Socials
{
    public class SocialConfirmPendingFriendRequest : SocialRequestHandler
    {
        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var myCharacter = request.Session.Character;
                var friendCharacter = Character.Get(request.Data.GetOrDefault<int>(k.friend));
                var accept = request.Data.GetOrDefault<int>(k.accept) == 1;

                Character.Exists(friendCharacter.Id).ThrowIfFalse(ErrorCodes.CharacterNotFound);

                var mySocial = myCharacter.GetSocial();
                var friendSocial = friendCharacter.GetSocial();
                var mySocialState = friendSocial.GetFriendSocialState(myCharacter);
                var friendSocialState = mySocial.GetFriendSocialState(friendCharacter);

                // milyen state van a masik oldalt?
                switch (mySocialState)
                {
                    // ha blokkoltak akkor error
                    case SocialState.Blocked:
                    {
                        throw new PerpetuumException(ErrorCodes.CharacterIsBlocked);
                    }
                    // ilyen nem lehet
                    case SocialState.Friend:
                    {
                        throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);
                    }
                }

                (friendSocialState != SocialState.FriendRequest && mySocialState != SocialState.PendingFriendRequest).ThrowIfTrue(ErrorCodes.WTFErrorMedicalAttentionSuggested);

                if (accept)
                {
                    mySocial.SetFriendSocialState(friendCharacter, SocialState.Friend).ThrowIfError();
                    friendSocial.SetFriendSocialState(myCharacter, SocialState.Friend).ThrowIfError();
                }
                else
                {
                    mySocial.RemoveFriend(friendCharacter);
                    friendSocial.RemoveFriend(myCharacter);
                }

                Transaction.Current.OnCommited(() =>
                {
                    CreateMessageToClient(Commands.SocialFriendRequestReply, mySocial).SetData("friend", friendCharacter.Id).SetData("accept", accept).Send();
                    CreateMessageToClient(Commands.SocialFriendRequestReply, friendSocial).SetData("friend", myCharacter.Id).SetData("accept", accept).Send();
                });

                scope.Complete();
            }
        }
    }
}