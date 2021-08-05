using Perpetuum.StateMachines;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem.Flocks;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem.Presences.PathFinders
{
    public class SpawnState : IState
    {
        protected readonly IRoamingPresence _presence;
        private TimeSpan _delay = TimeSpan.Zero;

        protected bool _spawning;
        protected bool _spawned;
        private double _repawnDelayModifier = 0.0;

        protected readonly int _playerMinDist;

        public SpawnState(IRoamingPresence presence, int playerMinDist = 200)
        {
            _presence = presence;
            _playerMinDist = playerMinDist;
        }

        public virtual void Enter()
        {
            _spawning = false;
            _spawned = false;

            _presence.SpawnOrigin = Position.Empty;
            _presence.CurrentRoamingPosition = Position.Empty;

            _elapsed = TimeSpan.Zero;

            _delay = TimeSpan.FromSeconds(_presence.Configuration.RoamingRespawnSeconds * _repawnDelayModifier);
            _repawnDelayModifier = FastRandom.NextDouble(1.0, 2.0);
        }

        public void Exit() { }

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
            if (_spawning)
                return;

            if (_spawned)
            {
                OnSpawned();
                return;
            }

            if (CheckElapsed(time))
                return;

            _spawning = true;
            Task.Run(() => SpawnFlocks()).ContinueWith(t =>
            {
                _spawned = true;
                _spawning = false;
            });
        }

        protected virtual void SpawnFlocks()
        {
            Position spawnPosition;
            bool anyPlayersAround;
            int range = _playerMinDist;

            do
            {
                spawnPosition = _presence.PathFinder.FindSpawnPosition(_presence).ToPosition();
                anyPlayersAround = _presence.Zone.Players.WithinRange(spawnPosition, range).Any();
                range--;
            } while (anyPlayersAround && range > 0);

            if (anyPlayersAround)
            {
                _presence.Log("FAILED to resolve spawn position out of range of players: " + spawnPosition);
                return;
            }

            DoSpawning(spawnPosition);
        }

        protected void DoSpawning(Position spawnPosition)
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

    public class NullRoamingState : IState
    {
        protected readonly IRoamingPresence _presence;

        public NullRoamingState(IRoamingPresence presence)
        {
            _presence = presence;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }

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

        private bool _finding;

        private bool IsAllNotIdle(Npc[] members)
        {
            var idleMembersCount = members.Select(m => m.AI.Current).OfType<IdleAI>().Count();
            return idleMembersCount < members.Length;
        }

        public override void Update(TimeSpan time)
        {
            if (_finding)
                return;

            var members = GetAllMembers();
            if (IsDeadAndExiting(members))
                return;

            if (IsAllNotIdle(members))
                return;

            _finding = true;
            Task.Run(() => FindNextRoamingPosition()).ContinueWith(t => _finding = false);
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
