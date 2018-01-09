using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;

namespace Perpetuum.Services.Standing
{
    public class StandingRepository : IStandingRepository
    {
        public int DeleteNeutralStandings()
        {
            return Db.Query().CommandText("delete standings where standing=0").ExecuteNonQuery();
        }

        public void InsertOrUpdate(StandingInfo info)
        {
            Db.Query().CommandText("setStanding")
                .SetParameter("@source", info.sourceEID)
                .SetParameter("@target", info.targetEID)
                .SetParameter("@standing", info.standing)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLExecutionError);
        }

        public void Delete(StandingInfo info)
        {
            Db.Query().CommandText("delete standings where source=@sourceEID and target=@targetEID")
                .SetParameter("@sourceEID", info.sourceEID)
                .SetParameter("@targetEID", info.targetEID)
                .ExecuteNonQuery();
        }

        private static StandingInfo CreateStandingInfoFromRecord(IDataRecord r)
        {
            var source = r.GetValue<long>("source");
            var target = r.GetValue<long>("target");
            var standing = r.GetValue<double>("standing");
            return new StandingInfo(source, target, standing);
        }

        public List<StandingInfo> GetAll()
        {
            return Db.Query().CommandText("select [source],[target],[standing] from standings")
                .Execute()
                .Select(CreateStandingInfoFromRecord).ToList();
        }

        public List<StandingInfo> GetStandingForCharacter(Character character)
        {
            var allianceEids = DefaultCorporationDataCache.GetMegaCorporationEids().ToArray();
            var qs = "select * from standings where [source] in (" + allianceEids.ArrayToString() + ") and [target]=@characterEid";
            return Db.Query().CommandText(qs).SetParameter("@characterEid", character.Eid).Execute().Select(CreateStandingInfoFromRecord).ToList();
        }

        public List<StandingLogEntry> GetStandingLogs(Character character, DateTimeRange timeRange)
        {
            var records = Db.Query().CommandText("select * from standinglog where characterid=@characterID and eventtime between @earlier and @later")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@earlier", timeRange.Start)
                .SetParameter("@later", timeRange.End)
                .Execute();

            var logEntries = new List<StandingLogEntry>();

            foreach (var record in records)
            {
                var logEntry = new StandingLogEntry();
                logEntry.characterID = record.GetValue<int>("characterid");
                logEntry.date = record.GetValue<DateTime>("eventtime");
                logEntry.actual = record.GetValue<double>("actual");
                logEntry.change = record.GetValue<double>("change");
                logEntry.allianceEID = record.GetValue<long>("allianceeid");
                logEntry.missionID = record.GetValue<int?>("missionid");

                logEntries.Add(logEntry);
            }

            return logEntries;
        }

        public void InsertStandingLog(StandingLogEntry logEntry)
        {
            Db.Query().CommandText("insert standinglog (characterid,actual,change,allianceeid,missionid) values (@characterID,@actual,@change,@allianceEID,@missionID)")
                .SetParameter("@characterID", logEntry.characterID)
                .SetParameter("@actual", logEntry.actual)
                .SetParameter("@change", logEntry.change)
                .SetParameter("@allianceEID", logEntry.allianceEID)
                .SetParameter("@missionID", logEntry.missionID)
                .ExecuteNonQuery();
        }
    }
}