using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Items
{
    public static class DefaultItemPropertyModifiers
    {
        private static readonly ILookup<int,ItemPropertyModifier> _defaultProperties;

        static DefaultItemPropertyModifiers()
        {
            _defaultProperties = Database.CreateLookupCache<int,ItemPropertyModifier>("aggregatevalues", "definition", r =>
            {
                var field = r.GetValue<AggregateField>("field");
                var value = r.GetValue<double>("value");
                return ItemPropertyModifier.Create(field,value);
            });
        }

        public static ItemPropertyModifier[] GetPropertyModifiers(int definition)
        {
            return _defaultProperties.GetOrEmpty(definition);
        }
    }
}