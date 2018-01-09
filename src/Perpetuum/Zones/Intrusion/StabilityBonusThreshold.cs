using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Intrusion
{
    public class StabilityBonusThreshold
    {
        public readonly StabilityBonusType bonusType;
        public readonly int threshold;
        public readonly EffectType effectType;

        public StabilityBonusThreshold(IDataRecord record)
        {
            bonusType = (StabilityBonusType)record.GetValue<int>("bonustype");
            threshold = record.GetValue<int>("threshold");
            effectType = (EffectType) (record.GetValue<int?>("effecttype") ?? 0);
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.categoryFlags, (long) CategoryFlags.cf_outpost}, 
                           {k.bonusType, (int) bonusType}, 
                           {k.threshold, threshold}, 
                           {k.effectType, (int) effectType}
                       };
        }

        public override string ToString()
        {
            return $"BonusType: {bonusType}, Threshold: {threshold}, EffectType: {effectType}";
        }
    }
}