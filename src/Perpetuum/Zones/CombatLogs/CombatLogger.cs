using System;
using System.Collections.Immutable;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;

namespace Perpetuum.Zones.CombatLogs
{
    public class CombatLogger
    {
        private readonly Player _owner;
        private readonly ICombatLogSaver _logSaver;
        private readonly CombatSummary.Factory _combatSummaryFactory;
        private ImmutableDictionary<Unit,CombatSummary> _summaries = ImmutableDictionary<Unit, CombatSummary>.Empty;

        public delegate CombatLogger Factory(Player owner);

        public CombatLogger(Player owner,ICombatLogSaver logSaver,CombatSummary.Factory combatSummaryFactory)
        {
            _owner = owner;
            _logSaver = logSaver;
            _combatSummaryFactory = combatSummaryFactory;
        }

        public Action Expired { get; set; }

        public void Log(Unit source, CombatEventArgs e)
        {
            _timer.Reset();
            var log = ImmutableInterlocked.GetOrAdd(ref _summaries, source, _ => _combatSummaryFactory(source));
            log.HandleCombatEvent(e);
        }

        private readonly TimeTracker _timer = new TimeTracker(TimeSpan.FromMinutes(5));

        public void Update(TimeSpan time)
        {
            _timer.Update(time);

            if (_timer.Expired)
            {
                Expired();
            }
        }

        public void Save(IZone zone,Unit killer)
        {
            try
            {
                _logSaver.Save(zone,_owner, killer,_summaries.Values);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }
    }
}