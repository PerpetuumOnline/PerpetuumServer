using Perpetuum.Log;
using Perpetuum.StateMachines;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem.Flocks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem.Presences.PathFinders
{
    public abstract class CancellableState
    {
        private static readonly TimeSpan MAX_WAIT = TimeSpan.FromSeconds(2);
        protected CancellationToken _token;
        private CancellationTokenSource _source;
        private Task _task;

        protected bool IsRunningTask { get; private set; }

        protected void OnEnter()
        {
            IsRunningTask = false;
            _source = new CancellationTokenSource();
            _token = _source.Token;
        }
        protected void OnExit()
        {
            if (IsRunningTask && _task != null)
            {
                Logger.Warning($"Cancelling task on RoamingState");
                _source.Cancel();
                _task.Wait(MAX_WAIT);
                Logger.Warning($"Cancelled!");
            }
        }
        protected bool IsCancelled => _token.IsCancellationRequested;

        protected void RunTask(Action action, Action<Task> continuation)
        {
            IsRunningTask = true;
            _task = Task.Run(action, _token).ContinueWith(continuation).ContinueWith(t => IsRunningTask = false);
        }
    }

    public class SpawnState : CancellableState, IState
    {
        protected readonly IRoamingPresence _presence;
        private TimeSpan _delay = TimeSpan.Zero;

        protected bool _spawned;
        private double _repawnDelayModifier = 0.0;

        protected readonly int _playerMinDist;

        public SpawnState(IRoamingPresence presence, int playerMinDist = 200)
        {
            _presence = presence;
            _playerMinDist = playerMinDist;
        }

        public void Enter()
        {
            OnEnter();
            _spawned = false;

            _presence.SpawnOrigin = Position.Empty;
            _presence.CurrentRoamingPosition = Position.Empty;

            _elapsed = TimeSpan.Zero;

            _delay = TimeSpan.FromSeconds(_presence.Configuration.RoamingRespawnSeconds * _repawnDelayModifier);
            _repawnDelayModifier = FastRandom.NextDouble(1.0, 2.0);
        }

        public void Exit()
        {
            OnExit();
        }

        protected virtual void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new RoamingState(_presence));
        }

        private TimeSpan _elapsed;

        private bool CheckElapsed(TimeSpan time)
        {
            _elapsed += time;
            return _elapsed < _delay;
        }

        //updated
        public void Update(TimeSpan time)
        {
            if (IsRunningTask)
                return;

            if (_spawned)
            {
                OnSpawned();
                return;
            }

            if (CheckElapsed(time))
                return;

            RunTask(() => SpawnFlocks(), t => _spawned = true);
        }

        protected virtual bool IsInRange(Position position, int range)
        {
            return _presence.Zone.Players.WithinRange(position, range).Any();
        }

        private void SpawnFlocks()
        {
            Position spawnPosition;
            bool anyPlayersAround;
            int range = _playerMinDist;

            do
            {
                if (IsCancelled)
                {
                    Logger.Warning("SpawnFlocks() cancelled");
                    return;
                }
                spawnPosition = _presence.PathFinder.FindSpawnPosition(_presence).ToPosition();
                anyPlayersAround = IsInRange(spawnPosition, range);
                range--;
            } while (anyPlayersAround && range > 0);

            if (anyPlayersAround)
            {
                _presence.Log("FAILED to resolve spawn position out of range of players: " + spawnPosition);
                return;
            }

            DoSpawning(spawnPosition);
        }

        private void DoSpawning(Position spawnPosition)
        {
            _presence.SpawnOrigin = spawnPosition;
            _presence.CurrentRoamingPosition = spawnPosition;
            _presence.Log("spawn position: " + spawnPosition);

            //spawn all flocks
            foreach (var flock in _presence.Flocks)
            {
                flock.SpawnAllMembers();
            }
        }
    }

    public class NullRoamingState : CancellableState, IState
    {
        protected readonly IRoamingPresence _presence;

        public NullRoamingState(IRoamingPresence presence)
        {
            _presence = presence;
        }

        public virtual void Enter() { OnEnter(); }
        public virtual void Exit() { OnExit(); }

        protected Npc[] GetAllMembers()
        {
            return _presence.Flocks.GetMembers().ToArray();
        }

        protected bool IsDeadAndExiting(Npc[] members)
        {
            if (members.Length <= 0)
            {
                _presence.StackFSM.Pop();
                return true;
            }
            return false;
        }

        public virtual void Update(TimeSpan time)
        {
            var members = GetAllMembers();
            IsDeadAndExiting(members);
        }
    }

    public class RoamingState : NullRoamingState
    {
        public RoamingState(IRoamingPresence presence) : base(presence) { }

        private bool IsAllNotIdle(Npc[] members)
        {
            var idleMembersCount = members.Select(m => m.AI.Current).OfType<IdleAI>().Count();
            return idleMembersCount < members.Length;
        }

        public override void Update(TimeSpan time)
        {
            if (IsRunningTask)
                return;

            var members = GetAllMembers();
            if (IsDeadAndExiting(members))
                return;

            if (IsAllNotIdle(members))
                return;

            RunTask(() => FindNextRoamingPosition(), t => { });
        }

        private void FindNextRoamingPosition()
        {
#if DEBUG
            _presence.Log("finding new roaming position. current: " + _presence.CurrentRoamingPosition);
#endif
            var nextRoamingPosition = _presence.PathFinder.FindNextRoamingPosition(_presence);
#if DEBUG
            _presence.Log("next roaming position: " + nextRoamingPosition + " dist:" + _presence.CurrentRoamingPosition.Distance(nextRoamingPosition));
#endif
            _presence.CurrentRoamingPosition = nextRoamingPosition;

            foreach (var npc in _presence.Flocks.GetMembers())
            {
                npc.HomePosition = _presence.CurrentRoamingPosition.ToPosition();
            }
        }
    }
}
