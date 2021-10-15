using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using Perpetuum.Zones.Teleporting;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence
{
    public class StaticSpawnState : SpawnState
    {
        private readonly int _baseRadius = 300;
        private readonly int _teleRadius = 150;
        public StaticSpawnState(IRoamingPresence presence, int playerMinDist = 200, int baseDist = 300, int teleportDist = 150) : base(presence, playerMinDist)
        {
            _baseRadius = baseDist;
            _teleRadius = teleportDist;
        }

        protected override void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new NullRoamingState(_presence));
        }

        protected override bool IsInRange(Position position, int range)
        {
            var zone = _presence.Zone;
            if (zone.Configuration.IsGamma && zone.IsUnitWithCategoryInRange(CategoryFlags.cf_pbs_docking_base, position, _baseRadius))
                return true;
            else if (zone.GetStaticUnits().OfType<DockingBase>().WithinRange2D(position, _baseRadius).Any())
                return true;
            else if (zone.GetStaticUnits().OfType<Teleport>().WithinRange2D(position, _teleRadius).Any())
                return true;
            else if (zone.PresenceManager.GetPresences().OfType<IRandomStaticPresence>().Where(p => p.SpawnOrigin.IsInRangeOf2D(position, _baseRadius)).Any())
                return true;

            return base.IsInRange(position, range);
        }
    }
}
