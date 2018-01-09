using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;

namespace Perpetuum.EntityFramework
{
    public sealed class EntityDefaultOptions
    {
        private readonly IDictionary<string, object> _dictionary = new Dictionary<string, object>();

        public EntityDefaultOptions() { }

        public EntityDefaultOptions(IDictionary<string,object> dictionary)
        {
            _dictionary = dictionary;
        }

        public int AlarmPeriod
        {
            get { return _dictionary.GetOrDefault<int>(k.alarmPeriod); }
        }

        public bool PublicBeam
        {
            get { return _dictionary.GetOrDefault<int>(k.publicBeam) == 1; }
        }

        public int Points
        {
            get { return _dictionary.GetOrDefault<int>(k.points); }
        }

        public int ProductionTime
        {
            get { return _dictionary.GetOrDefault<int>(k.productionTime); }
        }

        public int Level
        {
            get { return _dictionary.GetOrDefault<int>(k.level); }
        }

        public double PerSecondPrice
        {
            get { return _dictionary.GetOrDefault<double>(k.perSecondPrice); }
        }

        public double BulletTime
        {
            get { return _dictionary.GetOrDefault<double>(k.bulletTime); }
        }

        public string MineralLayer
        {
            get { return _dictionary.GetOrDefault<string>(k.mineral); }
        }

        public double Capacity
        {
            get { return _dictionary.GetOrDefault<double>(k.capacity); }
        }

        public int AmmoCapacity
        {
            get { return _dictionary.GetOrDefault<int>(k.ammoCapacity); }
        }

        public int ModuleFlag
        {
            get { return _dictionary.GetOrDefault<int>(k.moduleFlag); }
        }

        [NotNull]
        public int[] SlotFlags
        {
            get { return _dictionary.GetOrDefault<int[]>(k.slotFlags) ?? new int[0]; }
        }

        public Position SpawnPosition
        {
            get
            {
                var x = _dictionary.GetOrDefault<int>(k.spawnPositionX);
                var y = _dictionary.GetOrDefault<int>(k.spawnPositionY);
                return new Position(x,y);
            }
        }

        public int SpawnRange
        {
            get { return _dictionary.GetOrDefault<int>(k.spawnRange); }
        }

        public int Size
        {
            get { return _dictionary.GetOrDefault<int>(k.size); }
        }

        public int DockingRange
        {
            get { return _dictionary.GetOrDefault<int>(k.dockRange); }
        }

        public int Max
        {
            get { return _dictionary.GetOrDefault<int>(k.max); }
        }

        public int Increase => _dictionary.GetOrDefault<int>(k.increase);

        public double Height => _dictionary.GetOrDefault(k.height,1.0);

        public int Item => _dictionary.GetOrDefault<int>(k.item);

        [NotNull]
        public EffectType[] Effects
        {
            get { return _dictionary.GetOrDefault<int[]>(k.effect).Select(e => (EffectType)e).ToArray(); }
        }

        public int Type
        {
            get { return _dictionary.GetOrDefault<int>(k.type); }
        }

        public string Tier
        {
            get { return _dictionary.GetOrDefault<string>("tier"); }
        }

        public TechTreePointType KernelPointType
        {
            get { return _dictionary.GetOrDefault<TechTreePointType>("pointType"); }
        }

        public string ToGenxyString()
        {
            return GenxyConverter.Serialize(_dictionary);
        }

        public int ExtensionPoints
        {
            get
            {
                var ep = _dictionary.GetOrDefault("ep", 0);
                Debug.Assert(ep > 0);
                return ep;
            }
        }

        public int Credit
        {
            get
            {
                var credit = _dictionary.GetOrDefault("credit", 0);
                Debug.Assert(credit > 0);
                return credit;
            }
        }

        public int SparkID
        {
            get
            {
                var id = _dictionary.GetOrDefault("sparkId", 0);
                Debug.Assert(id > 0);
                return id;
            }
        }
    }
}