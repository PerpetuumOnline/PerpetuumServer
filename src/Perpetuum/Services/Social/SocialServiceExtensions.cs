using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Social
{
    public static class SocialServiceExtensions
    {
        public static void SendOnlineStateToFriends(this ICharacterSocial social, bool isOnline)
        {
            var data = new Dictionary<string, object>
            {
                {k.characterID, social.character.Id}, 
                {k.rootEID,social.character.Eid}
            };

            Message.Builder.SetCommand(isOnline ? Commands.ConnectionStart : Commands.ConnectionEnd)
                .WithData(data)
                .ToCharacters(social.GetFriends())
                .Send();
        }

        public static IEnumerable<Character> FilterWhoBlockedMe(this Character character, IEnumerable<Character> otherCharacters)
        {
            return otherCharacters.Where(otherCharacter => !otherCharacter.IsBlocked(character));
        }
    }
}