using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Players.ExtensionMethods;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Zones.Eggs
{
    /// <summary>
    /// Base class for a summonable object
    /// </summary>
    public abstract class Egg : Unit,IUsableItem
    {
        private readonly StackFSM _fsm;
        private Summoner[] _summoners = new Summoner[0];

        private UnitDespawnHelper _despawnHelper;

        protected Egg()
        {
            _fsm = new StackFSM();
            _fsm.Push(new WaitForSummonersState(this));
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected TimeSpan DespawnTime
        {
            set => _despawnHelper = UnitDespawnHelper.Create(this,value);
        }

        private Player[] SummonerPlayers
        {
            get { return _summoners.Select(s => s.SummonerPlayer).ToArray(); }
        }

        private IEnumerable<Summoner> Summoners => _summoners;

        protected abstract void OnSummonSuccess([NotNull] IZone zone, [NotNull] Player[] summoners);

        private void OnSummonSuccess()
        {
            try
            {
                var zone = Zone;
                if (zone != null)
                    OnSummonSuccess(zone, SummonerPlayers);
            }
            finally
            {
                RemoveFromZone();
            }
        }
     
        private void AddSummoner(Summoner summoner)
        {
            ImmutableInterlocked.Update(ref _summoners, current =>
            {
                if (current.Any(s => s.SummonerPlayer == summoner.SummonerPlayer))
                    return current;

                var updated = current.ToList();
                updated.Add(summoner);
                return updated.ToArray();
            });
        }

        private void RemoveSummoner(Summoner summoner)
        {
            ImmutableInterlocked.Update(ref _summoners, current => current.Where(s => s.SummonerPlayer != summoner.SummonerPlayer).ToArray());
        }

        private void ClearSummoners()
        {
            _summoners = new Summoner[0];
        }

        private Player OwnerPlayer
        {
            get { return Zone.GetPlayer(Owner); }
        }

        private TimeSpan SummonTime
        {
            get
            {
                if (ED.Config.activationTime != null)
                    return TimeSpan.FromMilliseconds((int) ED.Config.activationTime);

                Logger.Error("activationTime is not defined for " + Definition + " " + ED.Name);
                return TimeSpan.FromSeconds(30);
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _despawnHelper?.Update(time,this);

            _fsm.Update(time);
        }

        public void UseItem(Player player)
        {
            var summoner = new Summoner(this,player);
            summoner.Validate().ThrowIfError();
            AddSummoner(summoner);
        }

        private void StartProgressBar()
        {
            SummonerPlayers.SendStartProgressBar(this,SummonTime);
        }

        private void EndProgressBar()
        {
            SummonerPlayers.SendEndProgressBar(this,false);
        }

        private class WaitForSummonersState : IState
        {
            private readonly Egg _egg;
            private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(1));

            public WaitForSummonersState(Egg egg)
            {
                _egg = egg;
            }

            public void Enter()
            {
                _egg.ClearSummoners();
            }

            public void Exit()
            {

            }

            public void Update(TimeSpan time)
            {
                _timer.Update(time);
                if (!_timer.Passed)
                    return;

                _timer.Reset();

                foreach (var summoner in _egg.Summoners)
                {
                    if (summoner.Validate() != ErrorCodes.NoError)
                    {
                        _egg.RemoveSummoner(summoner);
                        continue;
                    }

                    summoner.SendBeamToPlayer(_timer.Interval);
                    _egg._fsm.Push(new BeginSummonState(_egg));
                    return;
                }
            }
        }

        private class BeginSummonState : IState
        {
            private readonly Egg _egg;
            private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(1));
            private TimeTracker _timerSummon;

            public BeginSummonState(Egg egg)
            {
                _egg = egg;
            }

            public void Enter()
            {
                _timerSummon = new TimeTracker(_egg.SummonTime);
                _egg.StartProgressBar();
            }

            public void Exit()
            {
                _egg.EndProgressBar();
            }

            public void Update(TimeSpan time)
            {
                _timer.Update(time).IsPassed(CheckSummoners);

                _timerSummon.Update(time);
                if (!_timerSummon.Expired)
                    return;

                _egg._fsm.Clear();
                _egg.OnSummonSuccess();
            }

            private void CheckSummoners()
            {
                foreach (var summoner in _egg.Summoners)
                {
                    if (summoner.Validate() != ErrorCodes.NoError)
                    {
                        _egg._fsm.Pop();
                        return;
                    }

                    summoner.SendBeamToPlayer(_timer.Interval);
                }
            }
        }

        private class Summoner
        {
            private readonly Egg _egg;
            private Position _summonerPosition;

            public Summoner(Egg egg, Player summoner)
            {
                _egg = egg;
                SummonerPlayer = summoner;
                _summonerPosition = summoner.CurrentPosition;
            }

            public Player SummonerPlayer { get; private set; }

            public void SendBeamToPlayer(TimeSpan duration)
            {
                _egg.Zone.CreateBeam(BeamType.loot_bolt, b => b.WithSource(SummonerPlayer)
                                         .WithTarget(_egg)
                                         .WithState(BeamState.Hit)
                                         .WithDuration(duration));
            }

            public ErrorCodes Validate()
            {
                if (SummonerPlayer.States.Dead || !SummonerPlayer.InZone)
                    return ErrorCodes.PlayerNotFound;

                if (!_egg.IsInRangeOf3D(SummonerPlayer, 10))
                    return ErrorCodes.ItemOutOfRange;

                if (!_summonerPosition.Equals(SummonerPlayer.CurrentPosition))
                    return ErrorCodes.WTFErrorMedicalAttentionSuggested;

                return ErrorCodes.NoError;
            }
        }
    }
}