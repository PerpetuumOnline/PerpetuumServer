using System.Collections.Immutable;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Social
{
    public class SocialService : ISocialService
    {
        private ImmutableDictionary<Character,ICharacterSocial> _socials = ImmutableDictionary<Character, ICharacterSocial>.Empty;

        public ICharacterSocial GetCharacterSocial(Character character)
        {
            if (character == Character.None)
                return CharacterSocial.None;

            var social = ImmutableInterlocked.GetOrAdd(ref _socials, character, CharacterSocial.LoadFromDb);
            return social;
        }
    }
}