using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Standing
{
    public interface IStandingHandler
    {
        bool TryGetStanding(long sourceEID, long targetEID, out double standing);
        void SetStanding(long sourceEID, long targetEID, double standing);

        IEnumerable<StandingInfo> GetReputationFor(long targetEID);

        IDictionary<string,object> GetStandingsList(long sourceEID);
        void ReloadStandingForCharacter(Character character);
        void WriteStandingLog(StandingLogEntry logEntry);
        List<StandingLogEntry> GetStandingLogs(Character character, DateTimeRange timeRange);
    }
}