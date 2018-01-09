using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor
    {
        /// <summary>
        /// Public function to obtaion members when we have a character
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public List<Character> GetGangMembersCached(Character character)
        {
            var gang = character.GetGang();
            return gang?.GetOnlineMembers().ToList() ?? new List<Character> {character};
        }
    }
}
