using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Effects
{
    public class EffectInfo
    {
        public readonly EffectType type;
        public readonly EffectCategory category;
        public readonly int duration;
        private readonly string _name;
        private readonly string _description;
        private readonly bool _isPositive;
        public readonly bool isAura;
        public readonly int auraRadius;
        private readonly int _displayFlags;

        public EffectInfo(IDataRecord record)
        {
            type = record.GetValue<EffectType>("id");
            category = (EffectCategory)record.GetValue<long>("effectcategory");
            _name = record.GetValue<string>("name");
            duration = record.GetValue<int>("duration");
            _description = record.GetValue<string>("description");
            _isPositive = record.GetValue<bool>("ispositive");
            isAura = record.GetValue<bool>("isaura");
            auraRadius = record.GetValue<int>("auraradius");
            _displayFlags = record.GetValue<int>("display");
        }

        public bool Display
        {
            get
            {
                return _displayFlags > 0;
            }
        }

        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                               {k.type, (int) type},
                               {k.name, _name},
                               {k.category, (long) category},
                               {k.duration, duration},
                               {k.description, _description},
                               {k.isPositive, _isPositive},
                               {k.isAura, isAura},
                               {k.auraRadius, auraRadius},
                               {k.display, _displayFlags}
                       };
        }
    }
}