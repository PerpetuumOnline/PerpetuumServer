using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using System;
using System.Drawing;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;

namespace Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence
{
    /// <summary>
    /// A non-roaming ExpiringPresence that would spawning with Roaming rules
    /// </summary>
    public class RandomSpawningExpiringPresence : ExpiringPresence, IRandomStaticPresence, IRoamingPresence
    {
        public StackFSM StackFSM { get; protected set; }
        public Position SpawnOrigin { get; set; }
        public IRoamingPathFinder PathFinder { get; set; }
        public override Area Area => Configuration.Area;
        public Point CurrentRoamingPosition { get; set; }

        public RandomSpawningExpiringPresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromSeconds((int)Configuration.DynamicLifeTime);

            InitStateMachine();
        }

        protected virtual void InitStateMachine()
        {
            StackFSM = new StackFSM();
            StackFSM.Push(new StaticSpawnState(this));
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            StackFSM?.Update(time);
        }

        protected override void OnPresenceExpired()
        {
            StackFSM?.Clear();
            foreach (var flock in Flocks)
            {
                flock.RemoveAllMembersFromZone(true);
            }
            ResetDynamicDespawnTimer();
            InitStateMachine();
            base.OnPresenceExpired();
        }

        public virtual void OnSpawned()
        {
            ResetDynamicDespawnTimer();
        }
    }
}
