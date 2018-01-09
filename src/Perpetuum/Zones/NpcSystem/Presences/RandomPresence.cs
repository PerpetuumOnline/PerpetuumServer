using System;
using System.Collections.Immutable;
using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class RandomPresence : Presence
    {
        private readonly IRandomFlockSelector _randomFlockSelector;
        private ImmutableHashSet<RandomFlockSpawner> _nextRandomFlocks = ImmutableHashSet<RandomFlockSpawner>.Empty;

        public RandomPresence(IZone zone,PresenceConfiguration configuration,IRandomFlockSelector randomFlockSelector) : base(zone,configuration)
        {
            _randomFlockSelector = randomFlockSelector;
        }

        public override void LoadFlocks()
        {
            for (var i = 0; i < Configuration.maxRandomFlock; i++)
            {
                var randomFlock = _randomFlockSelector.SelectRandomFlockByPresence(this);
                SpawnRandomFlock(randomFlock);
            }
        }

        private void SpawnRandomFlock(IFlockConfiguration flockConfiguration)
        {
            var flock = CreateAndAddFlock(flockConfiguration);
            flock.SpawnAllMembers();
        }

        protected override void OnFlockAdded(Flock flock)
        {
            flock.AllMembersDead += OnFlockAllMembersDead;
            base.OnFlockAdded(flock);
        }

        private void OnFlockAllMembersDead(Flock flock)
        {
            RemoveFlock(flock);

            ImmutableInterlocked.Update(ref _nextRandomFlocks, f =>
            {
                if (f.Count >= Configuration.maxRandomFlock)
                    return f;

                var randomFlockSpawner = new RandomFlockSpawner(this, _randomFlockSelector);
                return f.Add(randomFlockSpawner);
            });
        }

        protected override void OnUpdate(TimeSpan time)
        {
            foreach (var randomFlockSpawner in _nextRandomFlocks)
            {
                if ( randomFlockSpawner.Update(time) )
                {
                    ImmutableInterlocked.Update(ref _nextRandomFlocks, f => f.Remove(randomFlockSpawner));
                }
            }
        }

        public Position SpawnOriginForRandomPresence => Configuration.RandomCenter;

        //###################################

        private class RandomFlockSpawner
        {
            private readonly RandomPresence _presence;
            private readonly IFlockConfiguration _randomFlockConfiguration;
            private readonly TimeTracker _spawnTimer;

            public RandomFlockSpawner(RandomPresence presence,IRandomFlockSelector randomFlockSelector)
            {
                _presence = presence;
                _randomFlockConfiguration = randomFlockSelector.SelectRandomFlockByPresence(presence);
                _spawnTimer = new TimeTracker(_randomFlockConfiguration.RespawnTime);

                _presence.Log($"next random spawn for flock:{_randomFlockConfiguration.ID} {_randomFlockConfiguration.Name} {DateTime.Now}{_randomFlockConfiguration.RespawnTime}");
            }

            public bool Update(TimeSpan time)
            {
                _spawnTimer.Update(time);

                if (!_spawnTimer.Expired)
                    return false;

                _presence.SpawnRandomFlock(_randomFlockConfiguration);
                return true;
            }
        }
    }
}