using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Log;

namespace Perpetuum.Groups.Alliances
{
    public static class AllianceHelper
    {
        public static Dictionary<string, object> GetAllianceInfo()
        {
            var allianceEids = Db.Query().CommandText("select allianceEID from alliances where defaultalliance=1")
                                      .Execute()
                                      .Select(rec => rec.GetValue<long>(0));



            var dataRecords = Db.Query().CommandText("select allianceEID,name,defaultalliance,nick,active,logoresource from alliances where allianceEID in (" + allianceEids.ArrayToString() + ")")
                                     .Execute()
                                     .ToDictionary("c", record =>
            {
                var result = new Dictionary<string, object>
                {
                    {k.allianceEID, record.GetValue<long>(0)},
                    {k.name, record.GetValue<string>(1)}, 
                    {k.defaultAlliance, record.GetValue<bool>(2)}, 
                    {k.nick, record.GetValue<string>(3)}, 
                    {k.active, record.GetValue<bool>(4)},
                    {k.logoResource, record.GetValue<string>(5)}
                };

                //if private alliance AND active then return extra data
                if (!record.GetValue<bool>(2) && record.GetValue<bool>(4))
                {
                    result.Add(k.corporation, (from c in Db.Query().CommandText("select corporationEID from alliancemembers where allianceEID=@allianceEID").SetParameter("@allianceEID", record.GetValue<long>(0)).Execute() select c.GetValue<long>(0)).ToArray());
                    result.Add(k.delegateMember, (from r in Db.Query().CommandText("select memberID from allianceboard where allianceEID = @allianceEID and (role & @role) <> 0").SetParameter("@allianceEID", record.GetValue<long>(0)).SetParameter("@role", AllianceRole.alliance_delegate).Execute() select r.GetValue<int>(0)).ToArray());
                }

                return result;
            });

            return dataRecords;
        }

        public static bool AllianceNameOrNickTaken(string name, string nick)
        {
            var res = Db.Query().CommandText("select count(*) from alliances where [name]=@name or nick=@nick").SetParameter("@name", name).SetParameter("@nick", nick).ExecuteScalar<int>();

            return (res > 0) ? true : false;
        }

        public static bool IsAnyRole(AllianceRole role, params AllianceRole[] roles)
        {
            return roles.Any(r => (role & r) > 0);
        }

        public static AllianceRole GetAllRoles()
        {
            var result = 0;
            foreach (var v in Enum.GetValues(typeof (AllianceRole)))
            {
                result = result | (int) v;
            }

            return (AllianceRole) result;
        }

        public static Dictionary<string, object> AllianceRoleHistory(long allianceEid)
        {
            var result = new Dictionary<string, object>();
            var counter = 0;
            foreach (var record in Db.Query().CommandText("select issuerID,memberID,oldrole,newrole,rolesettime from alliancerolehistory where allianceEID=@allianceEID").SetParameter("@allianceEID", allianceEid).Execute())
            {
                var oneEntry = new Dictionary<string, object> {{k.issuerID, record.GetValue<int>(0)}, {k.memberID, record.GetValue<int>(1)}, {k.oldRole, record.GetValue<int>(2)}, {k.newRole, record.GetValue<int>(3)}, {k.date, record.GetValue<DateTime>(4)}};

                result.Add("c" + counter++, oneEntry);
            }

            return result;
        }

        public static ErrorCodes GetAllianceEidByFractionString(string fractionString, out long allianceEid)
        {
            allianceEid = 0L;
            var allianceName = "";
            switch (fractionString)
            {
                
                case "asi":
                    allianceName = "megacorp_ASI";
                    break;

                case "usa":
                case "tm":
                    allianceName = "megacorp_TM";
                    break;

                case "eu":
                case "ics":
                    allianceName = "megacorp_ICS";
                    break;

                default:
                    Logger.Error("no such fractionString: " + fractionString);
                    return ErrorCodes.NoSuchAlliance;
            }

            allianceEid = Db.Query().CommandText("select allianceEid from alliances where [name]=@allianceName").SetParameter("@allianceName", allianceName)
                .ExecuteScalar<long>();

            if (allianceEid <= 0)
            {
                Logger.Error("default alliance " + allianceName + " not defined. ");
                return ErrorCodes.NoSuchAlliance;

            }

            Logger.Info("alliance eid resolved " + allianceName + " => " + allianceEid);

            return ErrorCodes.NoError;
        }
    }
}