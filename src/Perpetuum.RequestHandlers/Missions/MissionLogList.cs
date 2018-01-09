using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionLogList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);

            Message.Builder.FromRequest(request).WithData(GetMissionLog(character, offsetInDays)).Send();
        }

        private static Dictionary<string, object> GetMissionLog(Character character, int offsetInDays)
        {
            var later = DateTime.Now.AddDays(-1 * offsetInDays);
            var earlier = later.AddDays(-14);

            //                                             0          1        2       3        4       5        
            var records = Db.Query().CommandText("select missionGuid,missionID,started,finished,succeeded,expire from missionlog where characterID=@characterID and finished is not null and started > @earlier and started < @later")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@earlier", earlier).SetParameter("@later", later)
                .Execute();

            return records.ToDictionary("c", RecordToMissionHistory);
        }

        private static Dictionary<string, object> RecordToMissionHistory(IDataRecord record)
        {
            var oneRecord = new Dictionary<string, object>
                       {
                           {k.guid, record.GetValue<Guid>(0).ToString()},
                           {k.missionID, record.GetValue<int>(1)},
                           {k.startTime, record.GetValue<DateTime>(2)},
                           {k.success, record.GetValue<bool>(4)},
                       };

            if (!record.IsDBNull(3)) oneRecord.Add(k.endTime, record.GetValue<DateTime>(3));
            if (!record.IsDBNull(5)) oneRecord.Add(k.expire, record.GetValue<DateTime>(5));

            return oneRecord;
        }
    }
}
