using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;
using Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence;
using System;

namespace Perpetuum.Zones.NpcSystem.Presences.GrowingPresences
{
    public interface IGrowingPresence
    {
        TimeSpan GrowTime { get; }
        IEscalatingPresenceFlockSelector Selector { get; }
        int CurrentGrowthLevel { get; }
    }
    public class GrowingPresence : RandomSpawningExpiringPresence, IGrowingPresence
    {
        public TimeSpan GrowTime { get; private set; }
        public virtual IEscalatingPresenceFlockSelector Selector { get; protected set; }
        public int CurrentGrowthLevel { get; protected set; }
        public GrowingPresence(IZone zone, IPresenceConfiguration configuration, IEscalatingPresenceFlockSelector selector) : base(zone, configuration)
        {
            Selector = selector;
            if (Configuration.GrowthSeconds != null)
                GrowTime = TimeSpan.FromSeconds((int)Configuration.GrowthSeconds);
        }

        protected override void InitStateMachine()
        {
            StackFSM = new StackFSM();
            StackFSM.Push(new GrowSpawnState(this));
        }

        public override void LoadFlocks()
        {
            for (var i = 0; i <= CurrentGrowthLevel; i++)
            {
                var flockConfigs = Selector.GetFlocksForPresenceLevel(ID, i);
                foreach (var config in flockConfigs)
                {
                    CreateAndAddFlock(config);
                }
            }
        }

        public void OnWaveSpawn(int level)
        {
            CurrentGrowthLevel = Math.Max(CurrentGrowthLevel, level);
        }

        protected override void OnPresenceExpired()
        {
            base.OnPresenceExpired();
            ClearFlocks();
            CurrentGrowthLevel = 0;
            LoadFlocks();
        }
    }
}
