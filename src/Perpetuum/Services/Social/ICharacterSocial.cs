using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Social
{
    public interface ICharacterSocial
    {
        Character character { get; }
        IEnumerable<Character> GetFriends();
        SocialState GetFriendSocialState(Character myCharacter);
        ErrorCodes SetFriendSocialState(Character friend, SocialState socialState, string note = null);
        void RemoveFriend(Character friendCharacter);
        IDictionary<string, object> ToDictionary();
    }
}