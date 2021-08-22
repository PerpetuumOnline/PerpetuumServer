using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using System;
using System.Linq;
using System.Drawing;
using Perpetuum.ExportedTypes;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence
{
    /// <summary>
    /// A non-roaming ExpiringPresence that would spawning with Roaming rules
    /// </summary>
    public class RandomSpawningExpiringPresence : ExpiringPresence, IRoamingPresence
    {
        public StackFSM StackFSM { get; }
        public Position SpawnOrigin { get; set; }
        public IRoamingPathFinder PathFinder { get; set; }
        public override Area Area => Configuration.Area;
        public Point CurrentRoamingPosition { get; set; }

        public RandomSpawningExpiringPresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromSeconds((int)Configuration.DynamicLifeTime);

            StackFSM = new StackFSM();
            StackFSM.Push(new StaticSpawnState(this));
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            StackFSM.Update(time);
        }

        protected override void OnPresenceExpired()
        {
            ResetDynamicDespawnTimer();
            foreach (var flock in Flocks)
            {
                flock.RemoveAllMembersFromZone(true);
            }
        }

        public void OnSpawned()
        {
            ResetDynamicDespawnTimer();
        }
    }

    public class StaticSpawnState : SpawnState
    {
        private readonly int BASE_RADIUS = 300;
        private readonly int PLAYER_RADIUS = 150;
        public StaticSpawnState(IRoamingPresence presence, int playerMinDist = 200) : base(presence, playerMinDist) { }

        protected override void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new NullRoamingState(_presence));
        }

        protected override bool IsInRange(Position position, int range)
        {
            var zone = _presence.Zone;
            if (zone.Configuration.IsGamma && zone.IsUnitWithCategoryInRange(CategoryFlags.cf_pbs_docking_base, position, BASE_RADIUS))
                return true;
            else if (zone.GetStaticUnits().OfType<DockingBase>().WithinRange2D(position, BASE_RADIUS).Any())
                return true;
            else if (zone.GetStaticUnits().OfType<Teleport>().WithinRange2D(position, PLAYER_RADIUS).Any())
                return true;
            else if (zone.PresenceManager.GetPresences().OfType<RandomSpawningExpiringPresence>().Where(p => p.SpawnOrigin.IsInRangeOf2D(position, BASE_RADIUS)).Any())
                return true;

            return zone.Players.WithinRange2D(position, PLAYER_RADIUS).Any();
        }
    }
}
