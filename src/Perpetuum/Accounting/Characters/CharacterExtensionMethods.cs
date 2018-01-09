using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Accounting.Characters
{
    public static class CharacterExtensionMethods
    {
        public static List<Character> ToCharacter(this IEnumerable<int> characterIds)
        {
            var result = new List<Character>();

            foreach (var characterId in characterIds)
            {
                var c = Character.Get(characterId);
                if (c == Character.None)
                    continue;

                result.Add(c);
            }

            return result;
        }

        public static IList<int> GetCharacterIDs(this IEnumerable<Character> characters)
        {
            return characters.Select(c => c.Id).ToArray();
        }
    }
}
