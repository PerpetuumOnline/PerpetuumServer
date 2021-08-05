using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Perpetuum.Comparers;
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
        Direct,
        EnWar,
        Ewar
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
        bool IsThreatened { get; }
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

        public bool IsThreatened
        {
            get { return !_hostiles.IsEmpty; }
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

    /// <summary>
    /// Manager of PseudoThreats
    /// Processes and manages an internal collection of players aggressive to an npc
    /// but not on the npc's ThreatManager.
    /// For awarding players a portion of the total ep reward.
    /// </summary>
    public interface IPseudoThreatManager
    {
        void Update(TimeSpan time);
        void AddOrRefreshExisting(Unit hostile);
        void Remove(Unit hostile);
        void AwardPseudoThreats(List<Unit> alreadyAwarded, IZone zone, int ep);
    }


    public class PseudoThreatManager : IPseudoThreatManager
    {
        private readonly List<PseudoThreat> _pseudoThreats;
        private readonly object _lock;

        public PseudoThreatManager()
        {
            _pseudoThreats = new List<PseudoThreat>();
            _lock = new object();
        }

        public void AwardPseudoThreats(List<Unit> alreadyAwarded, IZone zone, int ep)
        {
            var pseudoHostileUnits = new List<Unit>();
            lock (_lock)
            {
                pseudoHostileUnits = _pseudoThreats.Select(p => p.Unit).Except(alreadyAwarded, new EntityComparer()).Cast<Unit>().ToList();
            }
            foreach (var unit in pseudoHostileUnits)
            {
                var hostilePlayer = zone.ToPlayerOrGetOwnerPlayer(unit);
                hostilePlayer?.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Npc, ep / 2);
            }
        }

        public void AddOrRefreshExisting(Unit hostile)
        {
            lock (_lock)
            {
                var existing = _pseudoThreats.Where(x => x.Unit == hostile).FirstOrDefault();
                if (existing != null)
                {
                    existing.RefreshThreat();
                    return;
                }
                _pseudoThreats.Add(new PseudoThreat(hostile));
            }
        }

        public void Remove(Unit hostile)
        {
            lock(_lock)
                _pseudoThreats.RemoveAll(x => x.Unit == hostile);
        }

        public void Update(TimeSpan time)
        {
            lock (_lock)
            {
                foreach (var threat in _pseudoThreats)
                {
                    threat.Update(time);
                }
                CleanExpiredThreats();
            }
        }

        private void CleanExpiredThreats()
        {
            _pseudoThreats.RemoveAll(threat => threat.IsExpired);
        }
    }


    /// <summary>
    /// An expirable record of a player that is aggressing an npc but the npc is
    /// not capable of attacking back (removed from the ThreatManager)
    /// </summary>
    public class PseudoThreat
    {
        private TimeSpan _lastUpdated = TimeSpan.Zero;
        private TimeSpan Expiration = TimeSpan.FromMinutes(1);

        public PseudoThreat(Unit unit)
        {
            Unit = unit;
        }

        public Unit Unit { get; }

        public bool IsExpired
        {
            get { return _lastUpdated > Expiration; }
        }

        public void RefreshThreat()
        {
            _lastUpdated = TimeSpan.Zero;
        }

        public void Update(TimeSpan time)
        {
            _lastUpdated += time;
        }
    }
}
