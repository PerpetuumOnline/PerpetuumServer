using Perpetuum.Zones.NpcSystem.Flocks;
using System;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public class InterzonePresence : DynamicPresence
    {
        public InterzonePresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name.ToString());
            sb.Append(Configuration.ID.ToString());

            if (Zone != null)
                sb.Append(Zone.Id.ToString());

            return sb.ToString();
        }

        private void OnFlockRemoved()
        {
            if (Flocks.IsNullOrEmpty())
                OnPresenceExpired();
        }

        public override void LoadFlocks()
        {
            base.LoadFlocks();
            foreach(var flock in Flocks)
            {
                flock.SpawnAllMembers();
            }
        }
    }
}