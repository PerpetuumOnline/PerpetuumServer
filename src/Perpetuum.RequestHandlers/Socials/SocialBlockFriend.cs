using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Social;

namespace Perpetuum.RequestHandlers.Socials
{
    public class SocialBlockFriend : SocialRequestHandler
    {
        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var myCharacter = request.Session.Character;
                var friendCharacter = Character.Get(request.Data.GetOrDefault<int>(k.friend));

                Character.Exists(friendCharacter.Id).ThrowIfFalse(ErrorCodes.CharacterNotFound);

                var mySocial = myCharacter.GetSocial();

                mySocial.SetFriendSocialState(friendCharacter, SocialState.Blocked).ThrowIfError();

                var friendSocial = friendCharacter.GetSocial();
                var mySocialState = friendSocial.GetFriendSocialState(myCharacter);

                if (mySocialState != SocialState.Undefined && mySocialState != SocialState.Blocked)
                {
                    friendSocial.RemoveFriend(myCharacter);
                    Transaction.Current.OnCommited(() => CreateMessageToClient(request.Command, friendSocial).Send());
                }

                Transaction.Current.OnCommited(() => CreateMessageToClient(request.Command, mySocial).Send());

                scope.Complete();
            }
        }
    }
}