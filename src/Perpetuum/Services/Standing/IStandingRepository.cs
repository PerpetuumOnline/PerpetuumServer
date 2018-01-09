using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Standing
{
    public interface IStandingRepository
    {
        int DeleteNeutralStandings();

        void InsertOrUpdate(StandingInfo info);
        void Delete(StandingInfo info);
        List<StandingInfo> GetAll();
        List<StandingInfo> GetStandingForCharacter(Character character);

        void InsertStandingLog(StandingLogEntry logEntry);
        List<StandingLogEntry> GetStandingLogs(Character character, DateTimeRange timeRange);
    }
}