using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.EntityFramework
{
    public class EntityDefaultReader : IEntityDefaultReader
    {
        private readonly IExtensionReader _extensionReader;
        private Dictionary<int, EntityDefault> _entityDefaults;

        public EntityDefaultReader(IExtensionReader extensionReader)
        {
            _extensionReader = extensionReader;
        }

        public void Init()
        {
            _entityDefaults = LoadAll();
        }

        public bool Exists(int definition)
        {
            return Get(definition) != EntityDefault.None;
        }

        public EntityDefault Get(int definition)
        {
            return _entityDefaults.GetOrDefault(definition,EntityDefault.None);
        }

        public EntityDefault GetByEid(long eid)
        {
            var definition = Db.Query().CommandText("select definition from entities where eid = @eid")
                .SetParameter("@eid", eid)
                .ExecuteScalar<int>();

            return Get(definition);
        }

        public bool TryGet(int definition, out EntityDefault entityDefault)
        {
            entityDefault = Get(definition);
            return entityDefault != EntityDefault.None;
        }

        public IEnumerable<EntityDefault> GetAll()
        {
            return _entityDefaults.Values;
        }

        public int CountNonEnabledDefinitions()
        {
            var count = Db.Query().CommandText("select count(*) from entities where definition in (select definition from entitydefaults where enabled=0)").ExecuteScalar<int>();
            return count;
        }

        private Dictionary<int, EntityDefault> LoadAll()
        {
            var records = Db.Query().CommandText("select * from entitydefaults where enabled = 1").Execute();

            var definitionConfigs = LoadDefinitionConfigs();

            var result = new Dictionary<int, EntityDefault>();

            foreach (var record in records)
            {
                var definition = record.GetValue<int>("definition");

                var tierType = (TierType)(record.GetValue<int?>("tiertype") ?? 0);
                var tierLevel = record.GetValue<int?>("tierlevel") ?? 0;

                var entityDefault = new EntityDefault
                {
                    Volume = record.GetValue<double>("volume"),
                    _descriptionToken = record.GetValue<string>("descriptionToken"),
                    _hidden = record.GetValue<bool>("hidden"),
                    Definition = definition,
                    Name = record.GetValue<string>("definitionName"),
                    Quantity = record.GetValue<int>("quantity"),
                    AttributeFlags = new EntityAttributeFlags((ulong)record.GetValue<long>("attributeflags")),
                    CategoryFlags = (CategoryFlags)record.GetValue<long>("categoryflags"),
                    Mass = record.GetValue<double>("mass"),
                    Health = record.GetValue<double>("health"),
                    Purchasable = record.GetValue<bool>("purchasable"),
                    Options = new EntityDefaultOptions(((GenxyString)record.GetValue<string>("options")).ToDictionary()),
                    EnablerExtensions = GetEnablerAndRequiredExtensions(definition),
                    Config = definitionConfigs.GetOrDefault(definition, DefinitionConfig.None),
                    Tier = new TierInfo(tierType, tierLevel)
                };

                result[definition] = entityDefault;
            }

            return result;
        }

        private Dictionary<Extension,Extension[]> GetEnablerAndRequiredExtensions(int definition)
        {
            return _extensionReader.GetEnablerExtensions(definition).ToDictionary(e => e,e => _extensionReader.GetRequiredExtensions(e.id).ToArray());
        }

        private static Dictionary<int, DefinitionConfig> LoadDefinitionConfigs()
        {
            return Db.Query().CommandText("select * from definitionconfig").Execute().ToDictionary(r => r.GetValue<int>("definition"), r => new DefinitionConfig(r));
        }
    }
}