using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones.Locking
{
    public class LockHandler
    {
        private readonly Robot _owner;
        private readonly ItemProperty _lockingTime;
        private readonly ItemProperty _maxTargetingRange;
        private readonly ItemProperty _maxLockedTargets;
        private List<Lock> _locks = new List<Lock>();
        private readonly ConcurrentQueue<Lock> _newLocks = new ConcurrentQueue<Lock>();
        private readonly ConcurrentQueue<Lock> _removedLocks = new ConcurrentQueue<Lock>();
        private int _dirty;

        public LockHandler(Robot owner)
        {
            _owner = owner;
            _lockingTime = new LockingTimeProperty(owner);
            owner.AddProperty(_lockingTime);

            _maxTargetingRange = new MaxTargetingRangeProperty(owner);
            owner.AddProperty(_maxTargetingRange);

            _maxLockedTargets = new MaxLockedTargetsProperty(owner);
            owner.AddProperty(_maxLockedTargets);

            if (owner is IBlobableUnit)
            {
                owner.PropertyChanged += OnOwnerPropertyChanged;
            }
        }

        public double MaxTargetingRange { get { return _maxTargetingRange.Value; } }
        private int MaxLockedTargets { get { return (int) _maxLockedTargets.Value; } }
        public int Count { get { return _locks.Count; } }

        public List<Lock> Locks
        {
            get { return _locks; }
        }

        public bool HasFreeLockSlot
        {
            get { return Count < MaxLockedTargets; }
        }

        private void OnOwnerPropertyChanged(Item unit, ItemProperty property)
        {
            if (property.Field != AggregateField.blob_effect) 
                return;

            _lockingTime.Update();
            _maxTargetingRange.Update();
        }

        public void AddLock(long targetEid, bool isPrimary)
        {
            var targetUnit = _owner.Zone?.GetUnit(targetEid);
            if (targetUnit == null)
                return;

            AddLock(targetUnit,isPrimary);
        }

        public void AddLock(Unit target, bool isPrimary)
        {
            AddLock(new UnitLock(_owner) { Target = target, Primary = isPrimary });
        }

        public void AddLock(Lock newLock)
        {
            _newLocks.Enqueue(newLock);
            Interlocked.Exchange(ref _dirty, 1);
        }

        private void RemoveLock(Lock @lock)
        {
            _removedLocks.Enqueue(@lock);
            Interlocked.Exchange(ref _dirty, 1);
        }

        private void ProcessRemovedLocks(IList<Lock> locks)
        {
            Lock removedLock;
            while (_removedLocks.TryDequeue(out removedLock))
            {
                locks.Remove(removedLock);
            }
        }

        public event LockEventHandler LockStateChanged;

        private void OnLockStateChanged(Lock @lock)
        {
            if (@lock.State == LockState.Disabled)
            {
                RemoveLock(@lock);
            }

            LockStateChanged?.Invoke(@lock);
        }

        public event LockEventHandler<ErrorCodes> LockError;

        private void OnLockError(Lock @lock, ErrorCodes error)
        {
            LockError?.Invoke(@lock, error);
        }

        public void Update(TimeSpan time)
        {
            if (Interlocked.CompareExchange(ref _dirty, 0, 1) == 1)
            {
                var locks = _locks.ToList();
                try
                {
                    ProcessRemovedLocks(locks);
                    ProcessNewLocks(locks);
                }
                finally
                {
                    _locks = locks;
                }
            }
            
            foreach (var @lock in _locks)
            {
                @lock.Update(time);

                if (!ValidateLock(@lock))
                    @lock.Cancel();
            }
        }

        private void ProcessNewLocks(IList<Lock> locks)
        {
            Lock newLock;
            while (_newLocks.TryDequeue(out newLock))
            {
                if (locks.Any(l => l.Equals(newLock)))
                    continue;
                // if you are a tooladmin then lock up anything you want.
                // this is easier than making special bots to work with terrain.
                if (locks.Count >= MaxLockedTargets && _owner.GetCharacter().AccessLevel != AccessLevel.toolAdmin)
                {
                    OnLockError(newLock,ErrorCodes.MaxLockedTargetExceed);
                    continue;
                }

                if (!ValidateLock(newLock))
                    continue;

                if (newLock.Primary)
                {
                    var currentPrimaryLock = locks.FirstOrDefault(l => l.Primary);
                    if (currentPrimaryLock != null)
                        currentPrimaryLock.Primary = false;
                }



                locks.Add(newLock);

                newLock.Changed += OnLockStateChanged;

                var lockingTime = TimeSpan.Zero;

                var terrainLock = newLock as TerrainLock;
                if (terrainLock == null)
                    lockingTime = TimeSpan.FromMilliseconds(_lockingTime.Value);

                newLock.Start(lockingTime);
            }
        }

        private bool ValidateLock(Lock newLock)
        {
            var validator = new LockValidator(this);
            newLock.AcceptVisitor(validator);

            if (validator.Error == ErrorCodes.NoError)
                return true;

            OnLockError(newLock, validator.Error);
            return false;
        }

        public void SetPrimaryLock(Lock primaryLock)
        {
            var currentPrimaryLock = GetPrimaryLock();
            if (currentPrimaryLock != null)
            {
                if (currentPrimaryLock == primaryLock)
                    return;

                currentPrimaryLock.Primary = false;
            }

            if (primaryLock != null)
                primaryLock.Primary = true;
        }

        public void SetPrimaryLock(long lockId)
        {
            var newPrimaryLock = GetLock(lockId);
            SetPrimaryLock(newPrimaryLock);
        }

        [CanBeNull]
        public Lock GetLock(long lockId)
        {
            if (lockId == 0)
                return null;

            return _locks.FirstOrDefault(l => l.Id == lockId);
        }

        public void CancelLock(long lockId)
        {
            var l = GetLock(lockId);
            l?.Cancel();
        }

        public void ResetLocks()
        {
            foreach (var l in _locks)
            {
                l.Cancel();
            }
        }

        [CanBeNull]
        public Lock GetPrimaryLock()
        {
            return _locks.FirstOrDefault(l => l.Primary);
        }

        public bool IsLocked(Unit unit)
        {
            return _locks.OfType<UnitLock>().Any(l => unit.Eid == l.Target.Eid);
        }

        [CanBeNull]
        public UnitLock GetLockByUnit(Unit unit)
        {
            return GetLockByEid(unit.Eid);
        }

        [CanBeNull]
        private UnitLock GetLockByEid(long unitEid)
        {
            return _locks.OfType<UnitLock>().FirstOrDefault(l => l.Target.Eid == unitEid);
        }

        public bool IsInLockingRange(Unit target)
        {
            return IsInLockingRange(target.CurrentPosition);
        }

        public bool IsInLockingRange(Position targetPosition)
        {
            return _owner.CurrentPosition.IsInRangeOf3D(targetPosition, MaxTargetingRange);
        }

        private class LockingTimeProperty : UnitProperty
        {
            public LockingTimeProperty(Unit owner)
                : base(owner, AggregateField.locking_time,
                    AggregateField.locking_time_modifier,
                    // effektek
                    AggregateField.effect_sensor_booster_locking_time_modifier,
                    AggregateField.effect_sensor_dampener_locking_time_modifier,
                    AggregateField.effect_locking_time_modifier) { }

            protected override double CalculateValue()
            {
                var v = base.CalculateValue();

                var blobableUnit = owner as IBlobableUnit;
                blobableUnit?.BlobHandler.ApplyBlobPenalty(ref v, -5);

                return v;
            }
        }

        private class MaxLockedTargetsProperty : UnitProperty
        {
            public MaxLockedTargetsProperty(Unit owner) : base(owner, AggregateField.locked_targets_max)
            {
            }

            protected override double CalculateValue()
            {
                var v = base.CalculateValue();

                if (owner.GetCharacter() == Character.None)
                    return v;

                var lockedTargetsMaxBonus = owner.GetPropertyModifier(AggregateField.locked_targets_max_bonus);
                v = Math.Min(lockedTargetsMaxBonus.Value + 1, v);

                return v;
            }
        }

        private class MaxTargetingRangeProperty : UnitProperty
        {
            public MaxTargetingRangeProperty(Unit owner)
                : base(owner, AggregateField.locking_range,
                    AggregateField.locking_range_modifier,
                    //effektek
                    AggregateField.effect_sensor_booster_locking_range_modifier,
                    AggregateField.effect_sensor_dampener_locking_range_modifier,
                    AggregateField.effect_locking_range_modifier) { }

            protected override double CalculateValue()
            {
                var v = base.CalculateValue();

                var blobableUnit = owner as IBlobableUnit;
                blobableUnit?.BlobHandler.ApplyBlobPenalty(ref v, 0.5);

                return v;
            }
        }
    }
}