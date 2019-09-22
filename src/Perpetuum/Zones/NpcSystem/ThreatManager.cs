using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Perpetuum.Timers;
using Perpetuum.Units;

namespace Perpetuum.Zones.NpcSystem
{
    public enum ThreatType
    {
        Undefined,
        Bodypull,
        Damage,
        Support,
        Lock,
        Buff,
        Debuff,
        Direct
    }

    public struct Threat
    {

        public const double WEBBER = 25;
        public const double LOCK_PRIMARY = 2.0;
        public const double LOCK_SECONDARY = 1.0;
        public const double SENSOR_DAMPENER = 25.0;
        public const double BODY_PULL = 1.0;
        public const double SENSOR_BOOSTER = 15;
        public const double REMOTE_SENSOR_BOOSTER = 15;

        public readonly ThreatType type;
        public readonly double value;

        public Threat(ThreatType type, double value)
        {
            this.type = type;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{type} = {value}";
        }

        public static Threat Multiply(Threat threat, double multiplier)
        {
            return new Threat(threat.type,threat.value * multiplier);
        }
    }

    public class Hostile : IComparable<Hostile>
    {
        private static readonly TimeSpan _threatTimeOut = TimeSpan.FromSeconds(30);

        private double _threat;

        public readonly Unit unit;

        public TimeSpan LastThreatUpdate { get; private set; }

        public event Action<Hostile> Updated;

        public Hostile(Unit unit)
        {
            this.unit = unit;
            Threat = 0.0;
        }

        public void AddThreat(Threat threat)
        {
            if ( threat.value <= 0.0 )
                return;

            Threat += threat.value;
        }

        public bool IsExpired
        {
            get { return (GlobalTimer.Elapsed - LastThreatUpdate) >= _threatTimeOut; }
        }

        public double Threat
        {
            get { return _threat; }
            private set
            {
                if (Math.Abs(_threat - value) <= double.Epsilon)
                    return;

                _threat = value;

                OnThreatUpdated();
            }
        }

        private void OnThreatUpdated()
        {
            LastThreatUpdate = GlobalTimer.Elapsed;

            Updated?.Invoke(this);
        }

        public int CompareTo(Hostile other)
        {
            if (other._threat < _threat)
                return -1;

            if (other._threat > _threat)
                return 1;

            return 0;
        }
    }

    public interface IThreatManager
    {
        bool Contains(Unit hostile);
        void Remove(Hostile hostile);
        ImmutableSortedSet<Hostile> Hostiles { get; }
    }

    public static class ThreatExtensions
    {
        [CanBeNull]
        public static Hostile GetMostHatedHostile(this IThreatManager manager)
        {
            return manager.Hostiles.Min;
        }
    }

    public class ThreatManager : IThreatManager
    {
        private ImmutableDictionary<long,Hostile> _hostiles = ImmutableDictionary<long, Hostile>.Empty;

        public Hostile GetOrAddHostile(Unit unit)
        {
            return ImmutableInterlocked.GetOrAdd(ref _hostiles, unit.Eid, eid =>
            {
                var h = new Hostile(unit);
                return h;
            });
        }

        public ImmutableSortedSet<Hostile> Hostiles
        {
            get { return _hostiles.Values.ToImmutableSortedSet(); }
        }

        public bool Contains(Unit unit)
        {
            return _hostiles.ContainsKey(unit.Eid);
        }

        public void Clear()
        {
            _hostiles.Clear();
        }

        public void Remove(Hostile hostile)
        {
            ImmutableInterlocked.TryRemove(ref _hostiles, hostile.unit.Eid, out hostile);
        }

         public string ToDebugString()
        {
            if ( _hostiles.Count == 0 )
                return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("========== THREAT ==========");
            sb.AppendLine();

            foreach (var hostile in _hostiles.Values.OrderByDescending(h => h.Threat))
            {
                sb.AppendFormat("  {0} ({1}) => {2}", hostile.unit.ED.Name,hostile.unit.Eid, hostile.Threat);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("============================");

            return sb.ToString();
        }
    }
}
