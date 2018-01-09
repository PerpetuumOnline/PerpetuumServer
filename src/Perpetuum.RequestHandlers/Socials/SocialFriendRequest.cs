using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Social;

namespace Perpetuum.RequestHandlers.Socials
{
    public class SocialFriendRequest : SocialRequestHandler
    {
        public override void HandleRequest(IRequest request)
        {
            var myCharacter = request.Session.Character;
            var friendCharacter = Character.Get(request.Data.GetOrDefault<int>(k.friend));
            var reason = request.Data.GetOrDefault<string>(k.reason);

            using (var scope = Db.CreateTransaction())
            {
                Character.Exists(friendCharacter.Id).ThrowIfFalse(ErrorCodes.CharacterNotFound);

                var mySocial = myCharacter.GetSocial();
                var friendSocial = friendCharacter.GetSocial();

                // elkerjuk ,h mi van a masik oldalt
                var mySocialState = friendSocial.GetFriendSocialState(myCharacter);

                switch (mySocialState)
                {
                    // ha blokkol akkor hiba van
                    case SocialState.Blocked:
                    {
                        throw new PerpetuumException(ErrorCodes.TargetBlockedTheRequest);
                    }
                    // ha mar baratok akkor is hiba
                    case SocialState.Friend:
                    {
                        throw new PerpetuumException(ErrorCodes.CharacterAlreadyFriend);
                    }
                    // ha mar volt baratkero akkor automatikusan baratok lesznek, elvileg ilyen nem tortenhet
                    case SocialState.FriendRequest:
                    {
                        mySocial.SetFriendSocialState(friendCharacter, SocialState.Friend).ThrowIfError();
                        friendSocial.SetFriendSocialState(myCharacter, SocialState.Friend).ThrowIfError();
                        return;
                    }
                }

                mySocial.SetFriendSocialState(friendCharacter, SocialState.PendingFriendRequest, reason).ThrowIfError();
                friendSocial.SetFriendSocialState(myCharacter, SocialState.FriendRequest, reason).ThrowIfError();

                Transaction.Current.OnCommited(() =>
                {
                    CreateMessageToClient(Commands.SocialFriendRequest, friendSocial).SetData("sender", myCharacter.Id).Send();
                    // ha ok minden akkor kuldunk egy teljes listat
                    CreateMessageToClient(request.Command, mySocial).Send();
                });

                scope.Complete();
            }

        }
    }
}