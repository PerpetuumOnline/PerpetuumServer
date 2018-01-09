using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Groups.Corporations
{
    public class VolunteerCEO
    {
        public Character character;
        public DateTime expiry;
        public Corporation corporation;

        public override string ToString()
        {
            return $"Volunteer CEO characterID:{character.Id} expiry:{expiry} corporationEid:{corporation.Eid}";
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.characterID, character.Id},
                {k.expire, expiry}
            };
        }
    }
}