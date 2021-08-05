using System;
using System.Text;
using System.Drawing;
using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public class InterzoneRoamingPresence : InterzonePresence, IRoamingPresence
    {
        public StackFSM StackFSM { get; }
        public Position SpawnOrigin { get; set; }
        public Point CurrentRoamingPosition { get; set; }
        public IRoamingPathFinder PathFinder { get; set; }
        public InterzoneRoamingPresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromSeconds((int)Configuration.DynamicLifeTime);

            StackFSM = new StackFSM();
            StackFSM.Push(new SpawnState(this));
        }

        protected override void OnUpdate(TimeSpan time)
        {
            StackFSM.Update(time);
            base.OnUpdate(time);
        }

        public void OnSpawned()
        {
            ResetDynamicDespawnTimer();
        }
    }

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
            foreach (var flock in Flocks)
            {
                flock.SpawnAllMembers();
            }
        }
    }
}