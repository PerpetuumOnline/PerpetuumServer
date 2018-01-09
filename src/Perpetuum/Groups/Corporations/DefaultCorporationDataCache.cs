using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Groups.Corporations
{
    public static class DefaultCorporationDataCache
    {
        private static Dictionary<long, KeyValuePair<long, string>> _corporationToAlliance;
        private static Dictionary<long,string> _allianceEidToName;
        private static Dictionary<int, long> _raceIdToAlliance;

        public static void LoadAll()
        {
            _corporationToAlliance = LoadCorporationInfo();
            _allianceEidToName = LoadAllianceInfo();
            _raceIdToAlliance = LoadAllianceRaces();
        }

        private static Dictionary<long,KeyValuePair<long, string>> LoadCorporationInfo()
        {
            return Db.Query().CommandText("SELECT c.eid,a.allianceEID,c.name FROM corporations c JOIN dbo.alliancemembers a ON c.eid=a.corporationEID  WHERE c.defaultcorp=1")
                          .Execute()
                          .ToDictionary(r => r.GetValue<long>(0), r => new KeyValuePair<long, string>( r.GetValue<long>(1),r.GetValue<string>(2)));
        }

        public static IEnumerable<long> GetByAlliance(long allianceEid)
        {
            return  _corporationToAlliance.Where(r =>  r.Value.Key == allianceEid).Select(r=> r.Key);
        }

        public static IEnumerable<long> GetPureCorpsByAlliance(long allianceEid)
        {
            return _corporationToAlliance
                .Where(r => r.Value.Key == allianceEid && (r.Value.Value.EndsWith("_ww") || r.Value.Value.EndsWith("_ii") || r.Value.Value.EndsWith("_ss")))
                .Select(r => r.Key);
        }

        public static long GetIndustrialCorpByAlliance(long allianceEid)
        {
            return _corporationToAlliance
                .Where(r => r.Value.Key == allianceEid &&  r.Value.Value.EndsWith("_ii") )
                .Select(r => r.Key).First();
        }

        public static long GetPureCorporationFromAllianceByPostFix(long allianceEid, string postFix)
        {
            return _corporationToAlliance.Where(v => v.Value.Key == allianceEid && v.Value.Value.EndsWith(postFix))
                .Select(v=>v.Key)
                .FirstOrDefault();
        }

        public static IEnumerable<long> GetAllDefaultCorporationEid()
        {
            return _corporationToAlliance.Keys.ToList();
        }

        public static bool IsAllianceDefault(long allianceEid)
        {
            return _corporationToAlliance.Values.Any(a => a.Key == allianceEid);
        }

        public static bool IsCorporationDefault(long corporationEid)
        {
            return _corporationToAlliance.Keys.Any(c => c == corporationEid);
        }

        public static long GetAllianceEidByCorporationEid(long corporationEid)
        {
            KeyValuePair<long, string> kvp;
            return _corporationToAlliance.TryGetValue(corporationEid, out kvp) ? kvp.Key : 0;
        }

        private static Dictionary<long, string> LoadAllianceInfo()
        {
            return Db.Query().CommandText("select allianceeid,name from alliances where defaultalliance=1")
                .Execute()
                .ToDictionary(record => record.GetValue<long>(0), record => record.GetValue<string>(1));
        }

        private static Dictionary<int, long> LoadAllianceRaces()
        {
            return Db.Query().CommandText("select raceid,allianceEID from alliances where defaultalliance=1 AND name LIKE '%megacorp%'")
                .Execute()
                .ToDictionary(record => record.GetValue<int>(0), record => record.GetValue<long>(1));
        }

        /*
0   no race         
1	usa_great_corp
2	eu_great_corp
3	asia_great_corp
         */


        public static long GetAllianceEidByRace(int raceId)
        {
            return _raceIdToAlliance.GetOrDefault(raceId);
        }

        /// <summary>
        /// The default alliances
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<long> GetMegaCorporationEids()
        {
            return _allianceEidToName.Where(kvp => kvp.Value.StartsWith("megacorp")).Select(kvp => kvp.Key);
        }

        public static long SelectOpposingAlliance(long allianceEid)
        {
            var originalAllianceName = _allianceEidToName[allianceEid];

            if (originalAllianceName.Contains("TM"))
            {
                return GetAllianceEidByPostFix("ICS");
            }

            if (originalAllianceName.Contains("ICS"))
            {
                return GetAllianceEidByPostFix("ASI");
            }

            if (originalAllianceName.Contains("ASI"))
            {
                return GetAllianceEidByPostFix("TM");
            }

            return _allianceEidToName.Keys.FirstOrDefault();

        }

        private static long GetAllianceEidByPostFix(string postfix)
        {
            return _allianceEidToName
                .Where(kvp => kvp.Value.EndsWith(postfix))
                .Select(kvp => kvp.Key)
                .FirstOrDefault();

        }

        public static string GetAllianceName(long allianceEid)
        {
            return _allianceEidToName[allianceEid];
        }


        public static string GetCorporationName(long issuerCorporationEid)
        {
            var kvp = _corporationToAlliance[issuerCorporationEid];

            return kvp.Value;


        }
    }


}


