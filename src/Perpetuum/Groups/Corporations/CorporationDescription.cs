using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.GenXY;

namespace Perpetuum.Groups.Corporations
{
    public class CorporationDescription
    {
        public static readonly CorporationDescription None = new CorporationDescription();

        public long eid;
        public string name;
        public string nick;
        public double wallet;
        public int taxRate;
        public object publicProfile;
        public object privateProfile;
        public bool isDefault;
        public int? founder;
        public Character CEO;
        public DateTime creation;
        public long? allianceEID;
        public int? color;

        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>
            {
                {k.corporationEID, eid},
                {k.name, name},
                {k.defaultCorporation, isDefault},
                {k.publicProfile, publicProfile},
                {k.nick, nick},
                {k.wallet, (long)wallet},
                {k.privateProfile, privateProfile},
                {k.taxRate, taxRate},
                {k.CEO, CEO?.Id},
                {k.founderID, founder},
                {k.creation, creation},
                {k.allianceEID, allianceEID},
                {k.color, color}
            };
            return result;
        }

        [NotNull]
        public static CorporationDescription Get(long corporationEid)
        {
            const string selectCommandText = @"select name,taxrate,wallet,publicprofile,privateprofile,defaultcorp,nick,founder,creation,m.memberid,am.allianceEID,color from corporations c
                                               left join corporationmembers m on m.corporationeid=c.eid and (m.role & @ceorole) > 0
                                               left join alliancemembers am on am.corporationEID=c.eid
                                               where eid = @eid";

            var record = Db.Query().CommandText(selectCommandText)
                                 .SetParameter("@ceorole", (int)CorporationRole.CEO)
                                 .SetParameter("@eid", corporationEid)
                                 .ExecuteSingleRow();

            if (record == null)
                return None;

            var description = new CorporationDescription
            {
                eid = corporationEid,
                name = record.GetValue<string>(0),
                taxRate = record.GetValue<int>(1),
                wallet = record.GetValue<double>(2),

                publicProfile = null,
                privateProfile = null,

                isDefault = record.GetValue<bool>(5),
                nick = record.GetValue<string>(6),
                founder = record.GetValue<int?>(7),
                creation = record.GetValue<DateTime>(8),
                CEO = Character.Get(record.GetValue<int>(9)),
                allianceEID = record.GetValue<long?>(10),
                color = record.GetValue<int?>(11),
            };

            if (!record.IsDBNull(3))
                description.publicProfile = new GenxyString(record.GetValue<string>(3)).ToDictionary();

            if (!record.IsDBNull(4))
                description.privateProfile = new GenxyString(record.GetValue<string>(4)).ToDictionary();

            return description;
        }
    }
}