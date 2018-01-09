using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ExtensionService
{
    public struct ExtensionBonus
    {
        public readonly int extensionId;
        public readonly double bonus;
        public readonly AggregateField aggregateField;
        public readonly bool isEffectEnhancer;

        public ExtensionBonus(int extensionId,double bonus,AggregateField aggregateField)
        {
            this.extensionId = extensionId;
            this.bonus = bonus;
            this.aggregateField = aggregateField;
            isEffectEnhancer = false;
        }

        public ExtensionBonus(ExtensionInfo extensionInfo) : this(extensionInfo.id,extensionInfo.bonus,extensionInfo.aggregateField)
        {
        }


        public ExtensionBonus(IDataRecord record)
        {
            extensionId = record.GetValue<int>("extension");
            bonus = record.GetValue<double>("bonus");
            aggregateField = record.GetValue<AggregateField>("targetpropertyID");
            isEffectEnhancer = record.GetValue<bool>(k.effectEnhancer);
        }

        public bool Equals(ExtensionBonus other)
        {
            return other.extensionId == extensionId && other.bonus.Equals(bonus) && Equals(other.aggregateField, aggregateField);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (ExtensionBonus)) return false;
            return Equals((ExtensionBonus) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = extensionId;
                result = (result*397) ^ bonus.GetHashCode();
                result = (result*397) ^ aggregateField.GetHashCode();
                return result;
            }
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.extensionID, extensionId},
                           {k.bonus, bonus},
                           {k.aggregate, (int) aggregateField},
                           {k.effectEnhancer, isEffectEnhancer}
                       };
        }


    }

}
