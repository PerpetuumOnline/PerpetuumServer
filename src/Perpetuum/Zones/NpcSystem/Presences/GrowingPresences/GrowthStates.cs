using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using System;

namespace Perpetuum.Zones.NpcSystem.Presences.GrowingPresences
{
    public class GrowSpawnState : StaticSpawnState
    {
        private readonly GrowingPresence _growingPresence;
        public GrowSpawnState(GrowingPresence presence) : base(presence)
        {
            _growingPresence = presence;
        }

        protected override void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new GrowthState(_growingPresence));
        }
    }

    public class NPCBaseSpawnState : GrowSpawnState
    {
        private readonly GrowingNPCBasePresence _growingPresence;
        public NPCBaseSpawnState(GrowingNPCBasePresence presence) : base(presence)
        {
            _growingPresence = presence;
        }

        protected override void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new NPCBaseGrowthState(_growingPresence));
        }

        protected override void SetSpawnDelay()
        {
            if (_growingPresence.CurrentGrowthLevel == 0)
            {
                _repawnDelayModifier = FastRandom.NextDouble(1.0, 2.0);
            }
            _delay = TimeSpan.FromSeconds((int)_presence.Configuration.DynamicLifeTime * _repawnDelayModifier);
            _repawnDelayModifier = FastRandom.NextDouble(1.0, 2.0);
        }

        protected override bool IsValidSpawnPosition(Position position, int range)
        {
            if (!IsLocalRadiusClearForBase(position))
                return false;

            return !IsInRange(position, range);
        }
        private bool IsLocalRadiusClearForBase(Position center, int radius = 5)
        {
            var zone = _presence.Zone;
            if (zone == null)
                return false;

            int minX = (center.intX - radius).Clamp(0, zone.Size.Width - 1);
            int maxX = (center.intX + radius).Clamp(0, zone.Size.Width - 1);
            int minY = (center.intY - radius).Clamp(0, zone.Size.Height - 1);
            int maxY = (center.intY + radius).Clamp(0, zone.Size.Height - 1);

            var check = false;
            for (var j = minY; j < maxY; j++)
            {
                for (var i = minX; i < maxX; i++)
                {
                    var cPos = new Position(i, j);
                    if (!zone.Size.Contains(cPos.intX, cPos.intY))
                        continue;

                    var controlInfo = zone.Terrain.Controls.GetValue(cPos.intX, cPos.intY);
                    if (controlInfo.IsAnyTerraformProtected || !zone.Terrain.Slope.CheckSlope(cPos.intX, cPos.intY, ZoneExtensions.MIN_SLOPE))
                    {
                        return false;
                    }
                    check = true;
                }
            }
            return check;
        }

        protected override Position FindSpawnPosition()
        {
            return _presence.PathFinder.FindSpawnPosition(_presence).ToPosition();
        }
    }

    public class NPCBaseGrowthState : GrowthState
    {
        public NPCBaseGrowthState(GrowingNPCBasePresence presence) : base(presence)
        {
            _currentLevel = presence.CurrentGrowthLevel;
        }
    }

    public class GrowthState : NullRoamingState
    {
        private readonly GrowingPresence _growingPresence;
        private readonly TimeTracker _timer;
        protected int _currentLevel = 0;
        public GrowthState(GrowingPresence presence) : base(presence)
        {
            _growingPresence = presence;
            _timer = new TimeTracker(_growingPresence.GrowTime);
            _currentLevel = _growingPresence.CurrentGrowthLevel;
        }

        public override void Update(TimeSpan time)
        {
            if (IsRunningTask)
                return;

            var members = GetAllMembers();
            if (IsDeadAndExiting(members))
                return;

            if (!NextWaveReady(time))
                return;

            RunTask(() => SpawnNextWave(), t => { });
        }

        private bool NextWaveReady(TimeSpan time)
        {
            if (!CheckTimer(time))
                return false;

            _currentLevel++;
            return true;
        }

        private bool CheckTimer(TimeSpan time)
        {
            _timer.Update(time);
            if (!_timer.Expired)
                return false;

            _timer.Reset();
            return true;
        }

        private void SpawnNextWave()
        {
            var flockConfigs = _growingPresence.Selector.GetFlocksForPresenceLevel(_growingPresence.ID, _currentLevel);
            foreach (var config in flockConfigs)
            {
                var flock = _growingPresence.CreateAndAddFlock(config);
                flock.SpawnAllMembers();
            }
            _growingPresence.OnWaveSpawn(_currentLevel);
        }
    }
}
