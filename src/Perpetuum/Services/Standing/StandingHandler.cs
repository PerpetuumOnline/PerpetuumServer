using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Log;

namespace Perpetuum.Services.Standing
{
    public class StandingHandler : IStandingHandler
    {
        private readonly IStandingRepository _standingRepository;
        private ImmutableDictionary<long,StandingsHolder> _standingsHolders = ImmutableDictionary<long, StandingsHolder>.Empty;

        public StandingHandler(IStandingRepository standingRepository)
        {
            _standingRepository = standingRepository;
        }

        public bool TryGetStanding(long sourceEID, long targetEID, out double standing)
        {
            if (sourceEID == 0 || targetEID == 0)
            {
                standing = 0.0;
                return false;
            }

            var holder = _standingsHolders.GetOrDefault(sourceEID);
            if (holder != null)
                return holder.Standings.TryGetValue(targetEID, out standing);

            standing = 0.0;
            return false;
        }

        public void ReloadStandingForCharacter(Character character)
        {
            if ( character == Character.None )
                return;

            var infos = _standingRepository.GetStandingForCharacter(character);
            foreach (var info in infos)
            {
                GetOrAddStandingHolder(info.sourceEID).SetStanding(info.targetEID,info.standing);
            }
        }

        public void SetStanding(long sourceEID, long targetEID, double standing)
        {
            standing = standing.Clamp(-10.0, 10.0);
            var info = new StandingInfo(sourceEID, targetEID, standing);

            if (Math.Abs(standing) > double.Epsilon)
                _standingRepository.InsertOrUpdate(info);
            else
                _standingRepository.Delete(info);

            GetOrAddStandingHolder(sourceEID).SetStanding(targetEID, standing);
            SendStandingDataChangedToHosts(info);
        }

        public IDictionary<string, object> GetStandingsList(long sourceEID)
        {
            var holder = _standingsHolders.GetOrDefault(sourceEID);
            return holder?.ToDictionary();
        }

        public void WriteStandingLog(StandingLogEntry logEntry)
        {
            _standingRepository.InsertStandingLog(logEntry);
        }

        public List<StandingLogEntry> GetStandingLogs(Character character, DateTimeRange timeRange)
        {
            return _standingRepository.GetStandingLogs(character, timeRange);
        }

        public IEnumerable<StandingInfo> GetReputationFor(long targetEID)
        {
            foreach (var holder in _standingsHolders.Values)
            {
                if (holder.Standings.TryGetValue(targetEID,out double standing))
                {
                    yield return new StandingInfo(holder.sourceEID,targetEID,standing);
                }
            }
        }

        public void Init()
        {
            var res = _standingRepository.DeleteNeutralStandings();
            Logger.Info(res + " neutral standings deleted.");

            var infos = _standingRepository.GetAll();

            var builder = _standingsHolders.ToBuilder();

            foreach (var standingGroup in infos.GroupBy(i => i.sourceEID))
            {
                var holder = CreateStandingsHolder(standingGroup.Key);
                holder.AddMany(standingGroup);

                builder[standingGroup.Key] = holder;
            }

            _standingsHolders = builder.ToImmutable();

            Logger.Info("standings cached successfully. nofRelations:" + infos.Count);
        }

        private StandingsHolder GetOrAddStandingHolder(long sourceEID)
        {
            return ImmutableInterlocked.GetOrAdd(ref _standingsHolders, sourceEID, _ => CreateStandingsHolder(sourceEID));
        }

        private StandingsHolder CreateStandingsHolder(long sourceEID)
        {
            var holder = new StandingsHolder(sourceEID);
            holder.StandingUpdated += OnStandingUpdated;
            return holder;
        }

        private void OnStandingUpdated(StandingsHolder holder, long targetEID, double standing)
        {
            if (Math.Abs(standing) >= double.Epsilon)
                return;

            holder.Remove(targetEID);
            if (holder.Standings.Count == 0)
            {
                ImmutableInterlocked.TryRemove(ref _standingsHolders, holder.sourceEID, out holder);
            }
        }

        private static void SendStandingDataChangedToHosts(StandingInfo info)
        {
        }

        private class StandingsHolder
        {
            public readonly long sourceEID;
            private ImmutableDictionary<long,double> _standings = ImmutableDictionary<long, double>.Empty;

            public StandingsHolder(long sourceEID)
            {
                this.sourceEID = sourceEID;
            }

            public IReadOnlyDictionary<long, double> Standings
            {
                get { return _standings; }
            }

            public void AddMany(IEnumerable<StandingInfo> infos)
            {
                var builder = _standings.ToBuilder();
                foreach (var info in infos)
                {
                    builder[info.targetEID] = info.standing;
                }
                _standings = builder.ToImmutable();
            }

            public void SetStanding(long targetEID, double standing)
            {
                ImmutableInterlocked.AddOrUpdate(ref _standings,targetEID, standing, (t,s) => standing);
                OnStandingUpdated(targetEID,standing);
            }

            public void Remove(long targetEID)
            {
                ImmutableInterlocked.TryRemove(ref _standings,targetEID,out double standing);
            }

            public event Action<StandingsHolder,long,double> StandingUpdated;

            private void OnStandingUpdated(long targetEID,double standing)
            {
                StandingUpdated?.Invoke(this,targetEID,standing);
            }

            public IDictionary<string, object> ToDictionary()
            {
                return _standings.ToDictionary("c", s =>
                {
                    return new Dictionary<string, object>
                    {
                        {k.targetEID, s.Key},
                        {k.standing, s.Value}
                    };
                });
            }
        }
    }
}
