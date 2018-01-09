using System.Collections.Generic;

namespace Perpetuum.Groups.Alliances
{
    public class AllianceDescription
    {
        public long eid;
        public string name;
        public string nick;
        public bool isDefault;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.allianceEID, eid},
                {k.name, name},
                {k.nick, nick},
                {k.defaultAlliance, isDefault},
            };
        }
    }
}