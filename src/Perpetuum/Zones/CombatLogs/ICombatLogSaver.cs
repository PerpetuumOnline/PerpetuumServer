using System.Collections.Generic;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.CombatLogs
{
    public interface ICombatLogSaver
    {
        void Save(IZone zone, Player owner, Unit killer,IEnumerable<CombatSummary> summaries);
    }
}