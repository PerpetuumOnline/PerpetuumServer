using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Caching;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Log;

namespace Perpetuum.Groups.Corporations
{

    /// <summary>
    /// Represents an information about a corporation
    /// </summary>
    public class CorporationData
    {
        public readonly long eid;
        public readonly string name;
        public readonly bool isDefaultCorporation;
        public readonly IDictionary<string, object> publicProfile;
        public readonly string nick;
        public readonly bool active;
        public DateTime creation;
        public int? founder;
        public int? CEO;
        public long? allianceEid;
        public readonly int memberCount;
        public readonly int taxRate;
        public int? color;

        public readonly IDictionary<string, object> yellowPages;

        public static ICorporationManager CorporationManager { get; set; }

        private CorporationData(IDataRecord record)
        {   
            eid = record.GetValue<long>("eid");
            name = record.GetValue<string>("name");
            isDefaultCorporation = record.GetValue<bool>("defaultcorp");
            publicProfile = new GenxyString(record.GetValue<string>("publicprofile")).ToDictionary();
            nick = record.GetValue<string>("nick");
            active = record.GetValue<bool>("active");
            creation = record.GetValue<DateTime>("creation");
            founder = record.GetValue<int?>("founder");
            CEO = record.GetValue<int?>("ceo");
            allianceEid = record.GetValue<long?>("allianceeid");
            memberCount = record.GetValue<int>("membercount");
            taxRate = record.GetValue<int>("taxrate");
            color = record.GetValue<int?>("color");
            yellowPages = CorporationManager.GetYellowPages(eid);
        }

        public Dictionary<string,object> ToDictionary()
        {
            var result = new Dictionary<string, object>
                             {
                                     {k.corporationEID, eid},
                                     {k.name, name},
                                     {k.defaultCorporation, isDefaultCorporation},
                                     {k.publicProfile, publicProfile},
                                     {k.nick, nick},
                                     {k.active, active},
                                     {k.creation, creation},
                                     {k.founderID, founder},
                                     {k.CEO, CEO},
                                     {k.allianceEID, allianceEid},
                                     {k.memberCount, memberCount},
                                     {k.taxRate, taxRate},
                                     {k.color, color},
                                     {k.yellowPages, yellowPages}
                             };

            return result;
        }

        public static ObjectCache InfoCache { private get; set; }

        public static IDictionary<string, object> GetAnyInfoDictionary(IEnumerable<long> corporationEids)
        {
            return Select(corporationEids).ToDictionary("c", i => i.ToDictionary());
        }

        public static IEnumerable<CorporationData> Select(IEnumerable<long> corporationEids)
        {
            return corporationEids.Select(Get).Where(ci => ci != null);
        }

        [CanBeNull]
        public static CorporationData Get(long corporationEid)
        {
            return InfoCache.Get("c" + corporationEid, () =>
            {
                var ci = LoadCorporateInfo(corporationEid);
                if (ci == null)
                {
                    Logger.Warning("CorporationInfo cache. corporation not exists: " + corporationEid );
                }

                return ci;
            }, TimeSpan.FromHours(1));
        }

        public static void RemoveFromCache(long corporationEid)
        {
            InfoCache.Remove("c" + corporationEid);
        }

        public static void FlushCache()
        {
            InfoCache.Clear();
        }

        [CanBeNull]
        public static CorporationData LoadCorporateInfo(long corporationEiD)
        {
            if (corporationEiD == 0L)
                return null;
            
            const string sb = @"select eid,name,defaultcorp,publicprofile,nick,active,creation,founder,m.memberid as ceo,am.allianceEID as allianceeid,(select count(*) from corporationmembers where corporationEID=c.eid) as membercount, taxrate, color
                                        from corporations c
                                        left join corporationmembers m on m.corporationeid=c.eid and (m.role & @ceorole) > 0
                                        left join alliancemembers am on am.corporationEID=c.eid
                                        where eid=@corporationEid";

            var record = Db.Query().CommandText(sb)
                                 .SetParameter("@ceorole", (int) CorporationRole.CEO)
                                 .SetParameter("@corporationEid", corporationEiD)
                                 .ExecuteSingleRow();

            if (record == null)
                return null;

            return new CorporationData(record);
        }
    }
}
