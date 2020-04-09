using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Perpetuum.Log;
using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class Presence : INpcGroup
    {
        private ImmutableHashSet<Flock> _flocks = ImmutableHashSet<Flock>.Empty;

        public IPresenceConfiguration Configuration { get; private set; }

        [NotNull]
        public IZone Zone { get; private set; }

        public IFlockConfigurationRepository FlockConfigurationRepository { get; set; }

        public IEnumerable<Flock> Flocks => _flocks;

        public Presence(IZone zone, IPresenceConfiguration configuration)
        {
            Zone = zone;
            Configuration = configuration;
        }

        public FlockFactory FlockFactory { private get; set; }

        protected void ClearFlocks()
        {
            Flock[] removedFlocks = null;

            ImmutableInterlocked.Update(ref _flocks, f =>
            {
                removedFlocks = f.ToArray();
                return f.Clear();
            });

            foreach (var flock in removedFlocks)
            {
                flock.RemoveAllMembersFromZone(true);
            }
        }

        private void AddFlock(Flock flock)
        {
            ImmutableInterlocked.Update(ref _flocks, f => f.Add(flock));
            OnFlockAdded(flock);
        }

        public void RemoveFlock(Flock flock)
        {
            ImmutableInterlocked.Update(ref _flocks, f => f.Remove(flock));
            OnFlockRemoved(flock);
        }

        protected virtual void OnFlockAdded(Flock flock)
        {
            Log($"Flock added. {flock.Configuration.Name}");
            flock.NpcCreated += OnFlockNpcCreated;
        }

        private void OnFlockNpcCreated(Npc npc)
        {
            npc.SetGroup(this);
        }

        private void OnFlockRemoved(Flock flock)
        {
            flock.NpcCreated -= OnFlockNpcCreated;
            flock.RemoveAllMembersFromZone(true);
            Log($"Flock removed. {flock.Configuration.Name}");
        }

        private readonly IntervalTimer _updateTimer = new IntervalTimer(TimeSpan.FromSeconds(2));

        public void Update(TimeSpan time)
        {
            _updateTimer.Update(time);

            if (!_updateTimer.Passed)
                return;

            OnUpdate(_updateTimer.Elapsed);

            foreach (var flock in _flocks)
            {
                flock.Update(_updateTimer.Elapsed);
            }

            _updateTimer.Reset();
        }

        protected virtual void OnUpdate(TimeSpan time) { }

        public IDictionary<string, object> ToDictionary(bool withFlock = false)
        {
            var result = new Dictionary<string, object>
            {
                {k.ID, Configuration.ID},
                {k.name, Configuration.Name},
                {k.area, Area},
                {k.zoneID, Zone.Id},
                {k.roaming, Configuration.Roaming},
                {k.respawnSeconds, Configuration.RoamingRespawnSeconds},
                {k.presenceType,(int)Configuration.PresenceType},
                {k.radius, Configuration.RandomRadius},
                {k.x, Configuration.RandomCenterX},
                {k.y, Configuration.RandomCenterY},
            };

            if (withFlock)
            {
                var counter = 0;
                var dictionary = Flocks.Select(f => f.ToDictionary()).ToDictionary<IDictionary<string, object>, string, object>(o => "f" + counter++, o => o);
                result.Add(k.flock, dictionary);
            }

            return result;
        }

        protected void CreateAndAddFlocks(IEnumerable<IFlockConfiguration> configurations)
        {
            var builder = _flocks.ToBuilder();

            foreach (var configuration in configurations)
            {
                var flock = CreateFlock(configuration);
                builder.Add(flock);
                OnFlockAdded(flock);
            }

            _flocks = builder.ToImmutable();
        }

        protected Flock CreateAndAddFlock(int flockID)
        {
            var configuration = FlockConfigurationRepository.Get(flockID);
            return CreateAndAddFlock(configuration);
        }

        public Flock CreateAndAddFlock(IFlockConfiguration configuration)
        {
            var flock = CreateFlock(configuration);
            AddFlock(flock);
            return flock;
        }

        public Flock CreateFlock(IFlockConfiguration flockConfiguration)
        {
            return FlockFactory(flockConfiguration, this);
        }

        public virtual Area Area => Configuration.Area;

        public virtual void LoadFlocks()
        {
            var configs = FlockConfigurationRepository.GetAllByPresence(this).Where(t => t.Enabled);
            CreateAndAddFlocks(configs);
        }

        public override string ToString()
        {
            return $"{Name}:{Configuration.ID}:{Zone.Id}";
        }

        public string Name => Configuration.Name;

        public IEnumerable<Npc> Members
        {
            get { return _flocks.SelectMany(f => f.Members); }
        }

        public void AddDebugInfoToDictionary(IDictionary<string, object> dictionary)
        {

        }

        public virtual void Log(string message)
        {
            Logger.Info($"[Presence] ({ToString()}) - {message}");
        }
    }
}