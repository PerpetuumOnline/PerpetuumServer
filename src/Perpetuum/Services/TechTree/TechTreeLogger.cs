using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public abstract class TechTreeLogger
    {
        public static void WriteLog(LogEvent logEvent)
        {
            const string insertSqlCmd = @"insert into techtreelog 
                                          (logType,character,corporationEid,definition,quantity,pointType,amount) 
                                          values 
                                          (@logType,@character,@corporationEid,@definition,@quantity,@pointType,@amount)";

            Db.Query().CommandText(insertSqlCmd)
                .SetParameter("@logType", logEvent.Type)
                .SetParameter("@character", logEvent.Character.Id)
                .SetParameter("@corporationEid", logEvent.CorporationEid)
                .SetParameter("@definition", logEvent.Definition)
                .SetParameter("@quantity", logEvent.Quantity)
                .SetParameter("@pointType", logEvent.Points.type)
                .SetParameter("@amount",logEvent.Points.amount)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public IEnumerable<LogEvent> GetAll(DateTime from, DateTime to)
        {
            if ( to < from )
                ObjectHelper.Swap(ref from,ref to);

            var events = GetLogEventRecords(from, to).Select(CreateLogEventFromRecord);
            return events;
        }

        protected abstract IEnumerable<IDataRecord> GetLogEventRecords(DateTime from, DateTime to);

        private static LogEvent CreateLogEventFromRecord(IDataRecord record)
        {
            var type = (LogType)record.GetValue<int>("logType");
            var character = Character.Get(record.GetValue<int>("character"));

            var logEvent = new LogEvent(type,character)
            {
                CorporationEid = record.GetValue<long?>("corporationEid"),
                Definition = record.GetValue<int>("definition"),
                Quantity = record.GetValue<int>("quantity"),
                Points = new Points((TechTreePointType) record.GetValue<int>("pointType"),record.GetValue<int>("amount")),
                Created = record.GetValue<DateTime>("created")
            };

            return logEvent;
        }
    }
}