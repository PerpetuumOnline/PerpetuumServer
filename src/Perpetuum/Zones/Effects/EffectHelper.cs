using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Zones.Effects
{

    /// <summary>
    /// Effect helper class
    /// </summary>
    public static class EffectHelper
    {
        public static readonly IDictionary<EffectCategory, int> EffectCategoryLevels = Database.CreateCache<EffectCategory, int>("effectcategories", "flag", r => r.GetValue<int>("maxlevel"), null, key => (EffectCategory)(1L << (int)key));
        private static readonly IDictionary<EffectType, EffectInfo> _effectInfos = Database.CreateCache<EffectType, EffectInfo>("effects", "id", r => new EffectInfo(r));
        private static readonly ILookup<EffectType, ItemPropertyModifier> _effectDefaultModifiers;

        static EffectHelper()
        {
            _effectDefaultModifiers = Database.CreateLookupCache<EffectType, ItemPropertyModifier>("effectdefaultmodifiers", "effectid", r =>
                {
                    var field = (AggregateField)r.GetValue<int>("field");
                    var value = r.GetValue<double>("value");
                    return ItemPropertyModifier.Create(field, value);
                });
        }

        public static EffectInfo GetEffectInfo(EffectType effectType)
        {
            return _effectInfos[effectType];
        }

        public static IEnumerable<ItemPropertyModifier> GetEffectDefaultModifiers(EffectType effectType)
        {
            return _effectDefaultModifiers[effectType];
        }

        public static Dictionary<string, object> GetEffectInfosDictionary()
        {
            return _effectInfos.Values.ToDictionary("e", ei => ei.ToDictionary());
        }

        public static Dictionary<string, object> GetEffectDefaultModifiersDictionary()
        {
            var counter = 0;
            var result = new Dictionary<string, object>();
            foreach (var effectDefaultModifier in _effectDefaultModifiers)
            {
                var oneEntry = new Dictionary<string, object> {{k.effectType, (int) effectDefaultModifier.Key}};

                var effectsDict = new Dictionary<string, object>();
                
                foreach (var effect in effectDefaultModifier)
                {
                    effect.AddToDictionary(effectsDict);
                }

                oneEntry.Add(k.effect, effectsDict);
                result.Add("e"+counter++, oneEntry);
            }

            return result;
        }
    }
}