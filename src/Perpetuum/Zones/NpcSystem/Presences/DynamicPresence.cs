using System;
using System.Linq;
using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class DynamicPresence : Presence, INotifyPresenceExpired
    {
        private TimeTracker _lifeTimeTracker = new TimeTracker(TimeSpan.FromHours(1));

        public Position DynamicPosition { get; set; }

        public TimeSpan LifeTime
        {
            protected get { return _lifeTimeTracker.Duration; }
            set { _lifeTimeTracker = new TimeTracker(value); }
        }

        public event Action<Presence> PresenceExpired;

        public DynamicPresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromMilliseconds((int)Configuration.DynamicLifeTime);
        }

        public override Area Area
        {
            get { return Zone.Size.ToArea(); }
        }

        public void ResetDynamicDespawnTimer()
        {
            _lifeTimeTracker.Reset();
        }

        protected override void OnUpdate(TimeSpan time)
        {
            var x = Flocks.GetMembers().Any(m => m.AI.Current is AggressorAI);
            if (x)
                ResetDynamicDespawnTimer();

            _lifeTimeTracker.Update(time);

            if (_lifeTimeTracker.Expired)
            {
                OnPresenceExpired();
            }
        }

        protected virtual void OnPresenceExpired()
        {
            ClearFlocks();

            PresenceExpired?.Invoke(this);
        }
    }

    /// <summary>
    /// This is a Dynamic Presence that fires the expires event when its lifetime is up OR if all flocks have been killed
    /// It also has a customizable SpawnLocation that is separate from its home-point or origin.
    /// </summary>
    public class DynamicPresenceExtended : DynamicPresence
    {
        public Position SpawnLocation { get; set; }

        public DynamicPresenceExtended(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromSeconds((int)Configuration.DynamicLifeTime);
        }

        protected override void OnFlockAdded(Flock flock)
        {
            flock.AllMembersDead += OnFlockAllMembersDead;
            base.OnFlockAdded(flock);
        }

        private void OnFlockAllMembersDead(Flock flock)
        {
            flock.AllMembersDead -= OnFlockAllMembersDead;
            RemoveFlock(flock);
            OnFlockRemoved();
        }

        protected override void OnPresenceExpired()
        {
            foreach (var flock in Flocks)
            {
                flock.AllMembersDead -= OnFlockAllMembersDead;
            }
            base.OnPresenceExpired();
        }

        private void OnFlockRemoved()
        {
            if (Flocks.IsNullOrEmpty())
                OnPresenceExpired();
        }
    }
}