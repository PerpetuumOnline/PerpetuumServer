using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Log.Loggers;

namespace Perpetuum.Groups.Corporations.Loggers
{
    public class CorporationLogger
    {
        private readonly Corporation _corporation;
        private readonly ILogger<CorporationLogEvent> _logger;

        public delegate CorporationLogger Factory(Corporation corporation);

        public CorporationLogger(Corporation corporation)
        {
            _corporation = corporation;
            _logger = new DelegateLogger<CorporationLogEvent>(WriteLogEventToDb);
        }

        private static void WriteLogEventToDb(CorporationLogEvent e)
        {
            Db.Query().CommandText("insert into corporationlog (type,corporationEid,issuerId,memberId) values (@type,@corporationEid,@issuerId,@memberId)")
                .SetParameter("@type", (int) e.Type)
                .SetParameter("@corporationEid", e.Corporation.Eid)
                .SetParameter("@issuerId", e.Issuer.Id)
                .SetParameter("@memberId", e.Member.Id)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public IDictionary<string, object> GetHistory(int offsetInDays)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-2);

            const string sqlCmd = @"SELECT * FROM corporationlog
                                    WHERE corporationEid = @corporationEid AND timestamp between @earlier AND @later";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("corporationEid",_corporation.Eid)
                .SetParameter("@earlier",earlier)
                .SetParameter("@later",later)
                .Execute()
                .ToDictionary("c",r => new Dictionary<string, object>
                {
                    {k.timestamp,((IDataRecord) r).GetValue<DateTime>("timestamp")},
                    {k.type, ((IDataRecord) r).GetValue<int>("type")},
                    {k.issuerID,((IDataRecord) r).GetValue<int>("issuerId")},
                    {k.memberID,((IDataRecord) r).GetValue<int>("memberId")}
                });
            return result;
        }

        public void SetMemberRole(Character issuer, Character member)
        {
            _logger.Log(new CorporationLogEvent
            {
                Type = CorporationLogType.SetMemberRole,
                Corporation = _corporation,
                Issuer = issuer,
                Member = member
            });
        }
    }
}