using System;
using System.Collections.Generic;
using Perpetuum.Data;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public class MissionPayOutLogEntry
    {
        public int id;
        public DateTime evenTime;
        public Guid missionGuid;
        public int missionId;
        public MissionCategory missionCategory;
        public int missionLevel;
        public long? corporationEid;
        public int? characterId;
        public int gangSize;
        public double amount;
        public double sumAmount;

        public MissionPayOutLogEntry(int id,DateTime evenTime,Guid missionGuid,int missionId, MissionCategory missionCategory,int missionLevel, long? corporationEid, int? characterId, int gangSize,double amount, double sumAmount)
        {
            this.id = id;
            this.evenTime = evenTime;
            this.missionGuid = missionGuid;
            this.missionId = missionId;
            this.missionCategory = missionCategory;
            this.missionLevel = missionLevel;
            this.corporationEid = corporationEid;
            this.characterId = characterId;
            this.gangSize = gangSize;
            this.amount = amount;
            this.sumAmount = sumAmount;
        }

        public MissionPayOutLogEntry(Guid missionGuid, int missionId, MissionCategory missionCategory, int missionLevel, long? corporationEid, int? characterId,int gangSize, double amount, double sumAmount)
        {
            this.missionGuid = missionGuid;
            this.missionId = missionId;
            this.missionCategory = missionCategory;
            this.missionLevel = missionLevel;
            this.corporationEid = corporationEid;
            this.characterId = characterId;
            this.gangSize = gangSize;
            this.amount = amount;
            this.sumAmount = sumAmount;
        }

        public void SaveToDb()
        {
            var qs = @"INSERT dbo.missionpayoutlog
        ( 
          missionguid,
          missionid,
          missioncategory,
          missionlevel,
          corporationeid,
          characterid,
          gangsize,
          amount,
          sumamount
        )
VALUES  ( 
          @missionguid ,
          @missionid ,
          @missioncategory ,
          @missionLevel,
          @corporationeid ,
          @characterid ,
          @gangSize,
          @amount ,
          @sumamount
        )";

            var res = Db.Query().CommandText(qs)
                .SetParameter("@missionguid", missionGuid)
                .SetParameter("@missionid", missionId)
                .SetParameter("@missioncategory", (int)missionCategory)
                .SetParameter("@missionLevel" , missionLevel)
                .SetParameter("@corporationeid", corporationEid)
                .SetParameter("@characterid", characterId)
                .SetParameter("@gangSize", gangSize)
                .SetParameter("@amount", amount)
                .SetParameter("@sumamount", sumAmount)
                .ExecuteNonQuery();

            (res == 1).ThrowIfFalse(ErrorCodes.SQLInsertError);

        }

        public static void SaveLog(List<MissionPayOutLogEntry> payoutLogEntries)
        {
            foreach (var logEntry in payoutLogEntries)
            {
                logEntry.SaveToDb();
            }
        }
    }
}
