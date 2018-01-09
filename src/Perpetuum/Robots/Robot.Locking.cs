using System.Collections.Generic;
using System.Linq;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Robots
{
    partial class Robot
    {
        private LockHandler _lockHandler;

        private void InitLockHander()
        {
            _lockHandler = new LockHandler(this);
            _lockHandler.LockStateChanged += OnLockStateChanged;
            _lockHandler.LockError += OnLockError;
        }

        public bool IsLocked(Unit target)
        {
            return _lockHandler.IsLocked(target);
        }

        [CanBeNull]
        public Lock GetLock(long lockId)
        {
            return _lockHandler.GetLock(lockId);
        }

        [CanBeNull]
        public UnitLock GetLockByUnit(Unit unit)
        {
            return _lockHandler.GetLockByUnit(unit);
        }

        public Lock GetPrimaryLock()
        {
            return _lockHandler.GetPrimaryLock();
        }

        public bool IsInLockingRange(Unit unit)
        {
            return _lockHandler.IsInLockingRange(unit);
        }

        public void ResetLocks()
        {
            _lockHandler.ResetLocks();
        }

        public List<Lock> GetLocks()
        {
            return _lockHandler.Locks.ToList();
        }

        public bool HasFreeLockSlot
        {
            get { return _lockHandler.HasFreeLockSlot; }
        }

        public void AddLock(long targetEid, bool isPrimary)
        {
            _lockHandler.AddLock(targetEid, isPrimary);
        }

        public void AddLock(Unit target, bool isPrimary)
        {
            _lockHandler.AddLock(target, isPrimary);
        }

        public void AddLock(Lock newLock)
        {
            _lockHandler.AddLock(newLock);
        }

        public void SetPrimaryLock(long lockId)
        {
            _lockHandler.SetPrimaryLock(lockId);
        }

        public void SetPrimaryLock(Lock primaryLock)
        {
            _lockHandler.SetPrimaryLock(primaryLock);
        }

        public void CancelLock(long lockId)
        {
            _lockHandler.CancelLock(lockId);
        }

        public double MaxTargetingRange
        {
            get { return _lockHandler.MaxTargetingRange; }
        }

        public IEnumerable<Packet> GetLockPackets()
        {
            return GetLocks().Select(LockPacketBuilder.BuildPacket);
        }

        public void SubscribeLockEvents(LockEventHandler handler)
        {
            _lockHandler.LockStateChanged += handler;
        }

        public void UnsubscribeLockEvents(LockEventHandler handler)
        {
            _lockHandler.LockStateChanged -= handler;
        }

        public UnitLock GetFinishedPrimaryLock()
        {
            var unitLock = (UnitLock)_lockHandler.GetPrimaryLock();
            return unitLock?.State == LockState.Locked ? unitLock : null;
        }
    }
}
