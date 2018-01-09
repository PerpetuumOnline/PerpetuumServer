using System;
using System.Collections.Immutable;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Blobs.BlobEmitters;

namespace Perpetuum.Zones.Blobs
{
    public class BlobHandler<T> : IBlobHandler where T : Unit, IBlobableUnit
    {
        private readonly T _owner;
        private ImmutableHashSet<Unit> _units = ImmutableHashSet<Unit>.Empty;
        private readonly BlobEffectProperty _blobEffect;
        private double _blobLevel;
        private bool _isDirty;

        public BlobHandler(T owner)
        {
            _owner = owner;

            var blobLevelLow = new UnitProperty(_owner, AggregateField.blob_level_low, AggregateField.blob_level_low_modifier);
            _owner.AddProperty(blobLevelLow);

            var blobLevelHigh = new UnitProperty(_owner, AggregateField.blob_level_high, AggregateField.blob_level_high_modifier);
            _owner.AddProperty(blobLevelHigh);

            _blobEffect = new BlobEffectProperty(_owner, () => _blobLevel, () => blobLevelLow.Value, () => blobLevelHigh.Value);
            _owner.AddProperty(_blobEffect);
        }

        public void UpdateBlob(Unit unit)
        {
            var blobEmitter = unit as IBlobEmitter;
            if (blobEmitter == null)
                return;

            var blobbing = false;

            if (unit.InZone && !unit.States.Dead)
            {
                if (_owner.IsInRangeOf3D(unit, blobEmitter.BlobEmissionRadius))
                    blobbing = true;
            }

            var containsTarget = _units.Contains(unit);
            if (containsTarget)
            {
                if (!blobbing)
                {
                    ImmutableInterlocked.Update(ref _units, u => u.Remove(unit));
                    UpdateBlobLevel(blobEmitter, false);
                }
            }
            else
            {
                if (blobbing)
                {
                    ImmutableInterlocked.Update(ref _units, u => u.Add(unit));
                    UpdateBlobLevel(blobEmitter, true);
                }
            }
        }

        public void ApplyBlobPenalty(ref double v, double modifier)
        {
            _blobEffect.ApplyBlobPenalty(ref v, modifier);
        }

        private void UpdateBlobLevel(IBlobEmitter blobEmitter, bool enter)
        {
            if (enter)
                _blobLevel += blobEmitter.BlobEmission;
            else
                _blobLevel -= blobEmitter.BlobEmission;

            _blobLevel = Math.Max(0.0, _blobLevel);

            Logger.DebugInfo($"[{_owner.InfoString}] Update blob level: {_blobLevel}");
            _isDirty = true;
        }

        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(1));

        public void Update(TimeSpan time)
        {
            if (!_isDirty)
                return;

            _timer.Update(time);

            if (!_timer.Passed)
                return;

            _timer.Reset();
            _blobEffect.Update();
            _isDirty = false;
        }

        private class BlobEffectProperty : UnitProperty
        {
            private readonly Func<double> _blobLevel;
            private readonly Func<double> _blobLevelLow;
            private readonly Func<double> _blobLevelHigh;
            private double _multiplier;

            public BlobEffectProperty(T owner, Func<double> blobLevel, Func<double> blobLevelLow, Func<double> blobLevelHigh)
                : base(owner, AggregateField.blob_effect)
            {
                _blobLevel = blobLevel;
                _blobLevelLow = blobLevelLow;
                _blobLevelHigh = blobLevelHigh;
            }

            protected override double CalculateValue()
            {
                var blobLevel = _blobLevel();
                var blobLevelLow = _blobLevelLow();
                var blobLevelHigh = _blobLevelHigh();

                var top = Math.Max(blobLevelHigh - blobLevelLow, 0);

                //0 ... top
                var current = Math.Min(Math.Max(blobLevel - blobLevelLow, 0), top);

                if (top > 0.0)
                {
                    _multiplier = (current / top).Clamp();
                }

                var v = blobLevel.Clamp(0.0, blobLevelHigh);
                return v;
            }

            public void ApplyBlobPenalty(ref double v, double modifier)
            {
                v *= (1 - (_multiplier * modifier));
            }
        }
    }
}