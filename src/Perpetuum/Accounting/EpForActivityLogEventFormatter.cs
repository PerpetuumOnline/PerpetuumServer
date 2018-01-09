using Perpetuum.Common.Loggers;
using Perpetuum.Data;

namespace Perpetuum.Accounting
{
    public class EpForActivityLogger : DbLogger<EpForActivityLogEvent>
    {
        protected override void BuildCommand(EpForActivityLogEvent logEvent,DbQuery query)
        {
            const string q = @"INSERT dbo.epforactivitylog (accountid, characterid, epforactivitytype, rawpoints, points, boostfactor ) VALUES ( @accountId, @characterId, @epforactivityType, @rawPoints, @points, @boostFactor)";

            query.CommandText(q)
                .SetParameter("@accountId",logEvent.Account.Id)
                .SetParameter("@characterId", logEvent.CharacterId)
                .SetParameter("@epforactivityType", (int)logEvent.TransactionType)
                .SetParameter("@rawPoints", logEvent.RawPoints)
                .SetParameter("@points", logEvent.Points)
                .SetParameter("@boostFactor", logEvent.BoostFactor);
        }
    }
}