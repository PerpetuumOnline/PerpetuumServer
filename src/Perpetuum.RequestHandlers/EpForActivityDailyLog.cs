using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class EpForActivityDailyLog : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var fromOffset = request.Data.GetOrDefault<int>(k.from);
            var toOffset = request.Data.GetOrDefault<int>(k.duration);
            var dateFrom = DateTime.Now.AddDays(-fromOffset);
            var dateTo = dateFrom.AddDays(-toOffset);

            var log = new Dictionary<string, object>();

            var records =
                Db.Query().CommandText("epForActivityLogList")
                    .SetParameter("@accountId", request.Session.AccountId)
                    .SetParameter("@earlier", dateTo)
                    .SetParameter("@later", dateFrom)
                    .Execute();

            var count = 0;
            foreach (var record in records)
            {
                var oneEntry = new Dictionary<string, object>();

                var yearp = record.GetValue<int>("yearpart");
                var monthp = record.GetValue<int>("monthpart");
                var dayp = record.GetValue<int>("daypart");

                var eventTime = new DateTime(yearp, monthp, dayp, 12, 0, 0);
                var points = record.GetValue<int>("points");
                var activityType = record.GetValue<int>("epforactivitytype");
                var characterId = record.GetValue<int>("characterid");

                oneEntry[k.characterID] = characterId;
                oneEntry[k.points] = points;
                oneEntry[k.type] = activityType;
                oneEntry[k.time] = eventTime;

                log.Add("r" + count++, oneEntry);
            }

            var result = new Dictionary<string, object> {{"dailyLog", log}};

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}