using System.Linq;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones.NpcSystem
{
    public abstract class Creature : Robot
    {
        protected override void UpdateUnitVisibility(Unit target)
        {
            if (target is AreaBomb)
            {
                UpdateVisibility(target);
            }
        }

        protected internal override void UpdatePlayerVisibility(Player player)
        {
            UpdateVisibility(player);
        }

        protected override void OnUnitVisibilityUpdated(Unit target, Visibility visibility)
        {
            switch (visibility)
            {
                case Visibility.Visible:
                {
                    var robot = target as Robot;
                    robot?.SubscribeLockEvents(OnUnitLockStateChanged);

                    target.TileChanged += OnUnitTileChanged;
                    target.EffectChanged += OnUnitEffectChanged;
                    OnUnitTileChanged(target);
                    break;
                }
                case Visibility.Invisible:
                {
                    var robot = target as Robot;
                    robot?.UnsubscribeLockEvents(OnUnitLockStateChanged);

                    target.TileChanged -= OnUnitTileChanged;
                    target.EffectChanged -= OnUnitEffectChanged;
                    break;
                }
            }

            base.OnUnitVisibilityUpdated(target,visibility);
        }

        protected virtual void OnUnitEffectChanged(Unit unit, Effect effect, bool apply)
        {

        }

        protected virtual void OnUnitLockStateChanged(Lock @lock)
        {

        }

        protected virtual void OnUnitTileChanged(Unit unit)
        {
            
        }

        private const double PRIMARY_LOCK_CHANCE_FOR_SECONDARY_MODULE = 0.3;

        [CanBeNull]
        public UnitLock SelectOptimalLockTargetFor(ActiveModule module)
        {
            var locks = GetLocks().OfType<UnitLock>().Where(l =>
            {
                if (l.State != LockState.Locked)
                    return false;

                var isInOptimalRange = IsInRangeOf3D(l.Target, module.OptimalRange);
                return isInOptimalRange;

            }).ToArray();

            var primaryLock = locks.FirstOrDefault(l => l.Primary);

            if (module.ED.AttributeFlags.PrimaryLockedTarget)
                return primaryLock;

            var chance = FastRandom.NextDouble() <= PRIMARY_LOCK_CHANCE_FOR_SECONDARY_MODULE;
            if (primaryLock != null && chance)
                return primaryLock;

            return locks.RandomElement();
        }
    }
}