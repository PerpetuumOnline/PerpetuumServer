using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones.Locking
{
    public class LockValidator : LockVisitor
    {
        private readonly LockHandler _lockHandler;
        public ErrorCodes Error { get; private set; }

        public LockValidator(LockHandler lockHandler)
        {
            _lockHandler = lockHandler;
            Error = ErrorCodes.NoError;
        }

        public override void VisitUnitLock(UnitLock unitLock)
        {
            if (!unitLock.Target.InZone)
            {
                Error = ErrorCodes.LockTargetNotFound;
            }
            else if (unitLock.Target.States.Dead)
            {
                Error = ErrorCodes.TargetIsDead;
            }
            else if (!unitLock.Target.IsLockable)
            {
                Error = ErrorCodes.TargetIsNotLockable;
            }
            else if (!unitLock.Owner.IsInLockingRange(unitLock.Target))
            {
                Error = ErrorCodes.TargetOutOfRange;
            }
        }

        public override void VisitTerrainLock(TerrainLock terrainLock)
        {
            if (!_lockHandler.IsInLockingRange(terrainLock.Location))
            {
                Error = ErrorCodes.TargetOutOfRange;
            }
        }
    }
}