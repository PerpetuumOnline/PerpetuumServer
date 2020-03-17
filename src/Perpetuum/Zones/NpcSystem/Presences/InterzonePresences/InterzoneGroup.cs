using Perpetuum.Log;
using Perpetuum.Timers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public interface IInterzoneGroup
    {
        void Init(IZoneManager zoneManager, PresenceFactory presenceFactory);
        void AddConfigs(IEnumerable<IPresenceConfiguration> presences);
        void Update(TimeSpan elapsed);
        int GetId();
    }

    public class InterzoneGroup : IInterzoneGroup
    {
        private readonly int _id;
        private readonly string _name;
        private readonly int _respawnTime;
        private readonly double _respawnNoiseFactor;
        private IntervalTimer _timer;
        private IZoneManager _zoneManager;
        private PresenceFactory _presenceFactory;
        private readonly List<IPresenceConfiguration> _presenceConfigs = new List<IPresenceConfiguration>();
        private ImmutableList<Presence> _presences = ImmutableList<Presence>.Empty;

        public InterzoneGroup(int id, string name, int respawnTimeSeconds, double respawnNoise)
        {
            _id = id;
            _name = name;
            _respawnTime = respawnTimeSeconds;
            _respawnNoiseFactor = respawnNoise;
            ResetSpawnTimer();
        }

        public void AddConfigs(IEnumerable<IPresenceConfiguration> presences)
        {
            _presenceConfigs.AddMany(presences);
        }

        public void Init(IZoneManager zoneManager, PresenceFactory presenceFactory)
        {
            _zoneManager = zoneManager;
            _presenceFactory = presenceFactory;
            SpawnRandom();
        }

        private void SpawnRandom()
        {
            ResetSpawnTimer();
            ImmutableInterlocked.Update(ref _presences, p => p.RemoveAll(q => true));
            var config = GetRandom();
            var zone = _zoneManager.GetZone(config.ZoneID);
            var presence = _presenceFactory(zone, config);
            presence.LoadFlocks();
            ImmutableInterlocked.Update(ref _presences, p => p.Add(presence));
            if (presence is INotifyPresenceExpired notifier)
                notifier.PresenceExpired += OnPresenceExpired;
            _spawning = false;
            Logger.DebugInfo("IZ group spawned presence: " + presence.ToString());
        }

        private void OnPresenceExpired(Presence presence)
        {
            Logger.DebugInfo("IZ group presence expired/dead: " + presence.ToString());
            ImmutableInterlocked.Update(ref _presences, p => p.Remove(presence));
            if (presence is INotifyPresenceExpired notifier)
                notifier.PresenceExpired -= OnPresenceExpired;
        }

        private bool IsSpawned()
        {
            return !_presences.IsEmpty;
        }

        private void UpdatePresence(TimeSpan elapsed)
        {
            foreach (var presence in _presences)
            {
                presence.Update(elapsed);
            }
        }

        private bool _spawning = false;
        public void Update(TimeSpan elapsed)
        {
            Logger.DebugInfo(this.ToString());
            if (IsSpawned())
            {
                UpdatePresence(elapsed);
                return;
            }
            _timer.Update(elapsed);
            if (_timer.Passed && !_spawning)
            {
                _spawning = true;
                SpawnRandom();
            }
        }

        private void ResetSpawnTimer()
        {
            var randomTime = _respawnTime * (FastRandom.NextDouble(1.0 - _respawnNoiseFactor, 1.0 + _respawnNoiseFactor));
            _timer = new IntervalTimer(TimeSpan.FromSeconds(randomTime));
        }

        private IPresenceConfiguration GetRandom()
        {
            return _presenceConfigs.RandomElement();
        }

        public int GetId()
        {
            return _id;
        }

        public override string ToString()
        {
            return $"IZ Group: {_id} {_name} {IsSpawned()}, time to next spawn: {_timer.Interval}, elapsed:{_timer.Elapsed}, num presenses:{_presences.Count}";
        }

    }
}
