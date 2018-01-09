using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Zones.ProximityProbes
{
    public static class PBSRegisterHelper
    {
        public static IEnumerable<Character> GetRegisteredMembers(long eid)
        {
            return Db.Query().CommandText("select characterid from pbsregisteredmembers where eid=@eid")
                .SetParameter("@eid", eid)
                .Execute()
                .Select(r => Character.Get(r.GetValue<int>(0)));
        }

        public static void ClearMembersFromSql(long eid)
        {
            Db.Query().CommandText("delete pbsregisteredmembers where eid=@eid")
                .SetParameter("@eid", eid)
                .ExecuteNonQuery();
        }

        public static void WriteRegistersToDb(long probeEid, IEnumerable<Character> registeredCharacters)
        {
            foreach (var registeredCharacter in registeredCharacters)
            {
                Db.Query().CommandText("insert pbsregisteredmembers (eid, characterid) values (@eid,@characterID)")
                    .SetParameter("@eid", probeEid)
                    .SetParameter("@characterID", registeredCharacter.Id)
                    .ExecuteNonQuery();
            }
        }

        public static void DeleteRegisteredMembers(long probeEid, IEnumerable<Character> deletedCharacters)
        {
            var deletedStr = deletedCharacters.GetCharacterIDs().ArrayToString();

            Db.Query().CommandText("delete pbsregisteredmembers where eid=@eid and characterid in (" + deletedStr + ")")
                .SetParameter("@eid", probeEid)
                .ExecuteNonQuery();
        }

        public static IEnumerable<long> PBSRegGetEidsByRegisteredCharacter(Character character)
        {
            return Db.Query().CommandText("select distinct eid from pbsregisteredmembers where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .Execute()
                .Select(r => r.GetValue<long>(0)).ToArray();
        }
    }
}