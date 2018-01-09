using System;
using System.Linq;
using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class DynamicPresence : Presence,INotifyPresenceExpired
    {
        private TimeTracker _lifeTimeTracker = new TimeTracker(TimeSpan.FromHours(1));

        public Position DynamicPosition { get; set; }

        public TimeSpan LifeTime
        {
            protected get { return _lifeTimeTracker.Duration; }
            set { _lifeTimeTracker = new TimeTracker(value); }
        }

        public event Action<Presence> PresenceExpired;

        public DynamicPresence(IZone zone,PresenceConfiguration configuration) : base(zone,configuration)
        {
            if (Configuration.dynamicLifeTime != null) 
                LifeTime = TimeSpan.FromMilliseconds((int) Configuration.dynamicLifeTime);
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
}