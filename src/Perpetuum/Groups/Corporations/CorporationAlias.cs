using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;

namespace Perpetuum.Groups.Corporations
{
    public class CorporationAlias
    {
        public string name;
        public string nick;
        public DateTime eventTime;
        public int? characterId;


        public CorporationAlias(IDataRecord record)
        {
            name = record.GetValue<string>("name");
            nick = record.GetValue<string>("nick");
            eventTime = record.GetValue<DateTime>("eventtime");
            characterId = record.GetValue<int?>("characterid");
        }


        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>()
                {
                    {k.name, name},
                    {k.nick, nick},
                    {k.date, eventTime},
                    {k.characterID, characterId},
                };
        }


    }
}
