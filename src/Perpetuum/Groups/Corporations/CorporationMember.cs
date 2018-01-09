using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Groups.Corporations
{
    public struct CorporationMember
    {
        public Character character;
        public CorporationRole role;

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.characterID, character.Id},
                {k.role, (int) role}
            };
        }
    }
}
