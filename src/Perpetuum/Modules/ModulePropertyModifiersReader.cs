using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Modules
{
    public class ModulePropertyModifiersReader
    {
        private readonly IEntityDefaultReader _entityDefaultReader;
        private Dictionary<int,ILookup<AggregateField,AggregateField>> _modifiers;

        public ModulePropertyModifiersReader(IEntityDefaultReader entityDefaultReader)
        {
            _entityDefaultReader = entityDefaultReader;
        }

        public void Init()
        {
            var records = Db.Query().CommandText("select categoryflags,basefield,modifierfield from modulepropertymodifiers")
                .Execute()
                .Select(r => new
                {
                    categoryFlags = r.GetValue<CategoryFlags>("categoryflags"),
                    baseField = r.GetValue<AggregateField>("basefield"),
                    modifierField = r.GetValue<AggregateField>("modifierfield")
                })
                .Where(r => r.modifierField != AggregateField.undefined)
                .ToLookup(r => r.categoryFlags);


            var modules = _entityDefaultReader.GetAll().GetByCategoryFlags(CategoryFlags.cf_robot_equipment);
            _modifiers = new Dictionary<int,ILookup<AggregateField,AggregateField>>();

            foreach (var ed in modules)
            {
                var p = new List<KeyValuePair<AggregateField,AggregateField>>();

                foreach (var cf in ed.CategoryFlags.GetCategoryFlagsTree())
                {
                    foreach (var record in records.GetOrEmpty(cf))
                    {
                        p.Add(new KeyValuePair<AggregateField,AggregateField>(record.baseField,record.modifierField));
                    }

                    if (cf == CategoryFlags.cf_robot_equipment)
                        break;
                }

                _modifiers[ed.Definition] = p.ToLookup(kvp => kvp.Key,kvp => kvp.Value);
            }
        }

        public ILookup<AggregateField,AggregateField> GetModifiers(Module module)
        {
            return _modifiers.GetOrDefault(module.Definition);
        }
    }
}