using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Log;

namespace Perpetuum.Services.MarketEngine
{
    public class MarketTaxChangeLogger : ILogger<MarketTaxChangeLogEvent>
    {
        private readonly long _baseEid;
        private readonly ILogEventFormatter<MarketTaxChangeLogEvent,DbQuery> _formatter;

        public MarketTaxChangeLogger(Market market)
        {
            _baseEid = market.GetDockingBase().Eid;
            _formatter = new LogEventFormatter(market);
        }

        public IEnumerable<MarketTaxChangeLogEvent> GetHistory(TimeSpan offset, TimeSpan length)
        {
            var later = DateTime.Now - offset;
            var earlier = later - length;

            return Db.Query().CommandText("select * from markettaxlog where baseeid=@baseEID and eventtime between @earlier and @later")
                .SetParameter("@baseEID", _baseEid)
                .SetParameter("@earlier", earlier)
                .SetParameter("@later", later)
                .Execute()
                .Select(r => new MarketTaxChangeLogEvent()
                {
                    EventTime = r.GetValue<DateTime>("eventtime"),
                    Owner = r.GetValue<long>("owner"),
                    CharacterId = r.GetValue<int>("characterid"),
                    ChangeFrom = r.GetValue<double>("changefrom"),
                    ChangeTo = r.GetValue<double>("changeto"),
                    BaseEid = r.GetValue<long>("baseeid")
                }).ToArray();
        }

        public void Log(MarketTaxChangeLogEvent logEvent)
        {
            var query = _formatter.Format(logEvent);
            query.ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        private class LogEventFormatter : ILogEventFormatter<MarketTaxChangeLogEvent,DbQuery>
        {
            private readonly long _baseEid;

            public LogEventFormatter(Market market)
            {
                _baseEid = market.GetDockingBase().Eid;
            }
           
            public DbQuery Format(MarketTaxChangeLogEvent logEvent)
            {
                var query = Db.Query().CommandText("INSERT markettaxlog ( [owner], characterid, baseeid, changefrom, changeto) VALUES  ( @owner, @characterid, @baseeid, @changefrom, @changeto)")
                    .SetParameter("@owner", logEvent.Owner)
                    .SetParameter("@characterid", logEvent.CharacterId)
                    .SetParameter("@baseeid", _baseEid)
                    .SetParameter("@changefrom", logEvent.ChangeFrom)
                    .SetParameter("@changeto", logEvent.ChangeTo);
                              

                return query;
            }
        }
    }
}
