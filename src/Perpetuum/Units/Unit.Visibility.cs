using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Zones;

namespace Perpetuum.Units
{
    public interface IUnitVisibility
    {
        Unit Target { get; }
        LOSResult GetLineOfSight(bool ballistic);
    }

    partial class Unit
    {
        private ImmutableDictionary<long,UnitVisibility> _visibleUnits = ImmutableDictionary<long, UnitVisibility>.Empty;

        protected IReadOnlyCollection<IUnitVisibility> GetVisibleUnits()
        {
            return _visibleUnits.Values.ToArray();
        }

        public bool IsVisible(Unit target)
        {
            return _visibleUnits.ContainsKey(target.Eid);
        }

        [CanBeNull]
        public IUnitVisibility GetVisibility(Unit target)
        {
            return _visibleUnits.GetOrDefault(target.Eid);
        }

        protected virtual void UpdateUnitVisibility(Unit target)
        {
            // unit => unit nem latjak egymast
        }

        protected internal virtual void UpdatePlayerVisibility(Player player)
        {
            // unit => player nem latjak egymast
        }

        public virtual void UpdateVisibilityOf(Unit target)
        {
            target.UpdateUnitVisibility(this);
        }

        protected void UpdateVisibility(Unit target)
        {
            var visibility = Visibility.Invisible;

            if (InZone && target.InZone)
            {
                if ( IsDetected(target) )
                    visibility = Visibility.Visible;
            }

            UnitVisibility info;
            if (!_visibleUnits.TryGetValue(target.Eid, out info))
            {
                if (visibility == Visibility.Visible)
                {
                    info = new UnitVisibility(this, target);
                    ImmutableInterlocked.TryAdd(ref _visibleUnits, target.Eid, info);
                    OnUnitVisibilityUpdated(target, Visibility.Visible);
                }
            }
            else
            {
                if (visibility == Visibility.Invisible)
                {
                    UnitVisibility v;
                    if ( ImmutableInterlocked.TryRemove(ref _visibleUnits,target.Eid,out v))
                        OnUnitVisibilityUpdated(target, Visibility.Invisible);
                }
            }

            if (info != null && visibility == Visibility.Visible)
                info.ResetLineOfSight();
        }

        protected virtual void OnUnitVisibilityUpdated(Unit target, Visibility visibility)
        {
            Logger.DebugInfo(InfoString + " => " + target.InfoString + " => " + visibility);
        }

        protected virtual bool IsDetected(Unit target)
        {
            var robot = target as Robot;
            if (robot != null)
            {
                if (robot.IsLocked(this))
                    return true;
            }

            var range = 100 / target.StealthStrength * DetectionStrength;
            return IsInRangeOf3D(target, range);
        }

        public List<T> GetWitnessUnits<T>() where T : Unit
        {
            var result = new List<T>();

            var zone = Zone;
            if (zone == null)
                return result;

            foreach (var unit in zone.Units.OfType<T>())
            {
                if (unit.IsVisible(this))
                    result.Add(unit);
            }

            return result;
        }

        protected IEnumerable<Unit> GetUnitsWithinRange2D(double range)
        {
            return Zone.GetUnitsWithinRange2D(CurrentPosition, range);
        }

        private class UnitVisibility : IUnitVisibility
        {
            private readonly Unit _source;
            private ExpiringLosHolder _linearLos;
            private ExpiringLosHolder _ballisticLos;

            public UnitVisibility(Unit source, Unit unit)
            {
                _source = source;
                Target = unit;
            }

            public void ResetLineOfSight()
            {
                _linearLos = null;
                _ballisticLos = null;
            }

            public Unit Target { get; }

            public LOSResult GetLineOfSight(bool ballistic)
            {
                return ballistic ? GetLineOfSight(ref _ballisticLos,true) : GetLineOfSight(ref _linearLos,false);
            }

            private LOSResult GetLineOfSight(ref ExpiringLosHolder losHolder, bool ballistic)
            {
                var h = losHolder;
                if (h != null && h.Expired)
                {
                    losHolder = null;
                    Logger.DebugWarning("LOS expired");
                }

                var holder = LazyInitializer.EnsureInitialized(ref losHolder, () =>
                {
                    var losResult = _source.Zone.IsInLineOfSight(_source, Target, ballistic);
                    return new ExpiringLosHolder(losResult, TimeSpan.FromSeconds(4));
                });

                return holder.losResult;
            }

            private class ExpiringLosHolder
            {
                public readonly LOSResult losResult;
                private readonly DateTime _expiry;

                public ExpiringLosHolder(LOSResult losResult, TimeSpan lifetime)
                {
                    this.losResult = losResult;
                    _expiry = DateTime.Now.Add(lifetime);
                }

                public bool Expired => DateTime.Now >= _expiry;
            }
        }
    }
}
