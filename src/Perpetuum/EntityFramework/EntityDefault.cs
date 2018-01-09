using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.EntityFramework
{
    public sealed class EntityDefault
    {
        public static readonly EntityDefault None = new EntityDefault();

        public static IEntityDefaultReader Reader { get; set; }

        public EntityDefault()
        {
            Options = new EntityDefaultOptions();
            EnablerExtensions = new Dictionary<Extension, Extension[]>();
            Config = DefinitionConfig.None;
        }

        public string _descriptionToken;
        public bool _hidden;

        public int Definition { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public EntityAttributeFlags AttributeFlags { get; set; }
        public CategoryFlags CategoryFlags { get; set; }
        public double Mass { get; set; }
        public double Health { get; set; }
        public double Volume { get; set; }
        public bool Purchasable { get; set; }

        public Dictionary<Extension /* enabler */,Extension[] /* required */> EnablerExtensions { get; set; }

        public EntityDefaultOptions Options { get; set; }

        [NotNull]
        public DefinitionConfig Config { get; set; }
        public TierInfo Tier { get; set; }

        public static IEnumerable<EntityDefault> All => Reader.GetAll();

        public bool IsSellable
        {
            get
            {
                if (CategoryFlags.IsCategory(CategoryFlags.cf_documents)) 
                    return false;

                //... other category flags here...

                if (!Purchasable) return false;
                //... other conditions

                return true;
            }
        }

        public static bool Exists(int definition)
        {
            return Reader.Exists(definition);
        }

        public static bool TryGet(int definition, out EntityDefault entityDefault)
        {
            return Reader.TryGet(definition, out entityDefault);
        }

        [NotNull]
        public static EntityDefault GetByName(string name)
        {
            return Reader.GetByName(name);
        }

        public static EntityDefault GetByEid(long eid)
        {
            return Reader.GetByEid(eid);
        }

        [NotNull]
        public static EntityDefault GetOrThrow(int definition)
        {
            return Get(definition).ThrowIfEqual(None, ErrorCodes.DefinitionNotSupported);
        }

        public static EntityDefault Get(int definition)
        {
            return Reader.Get(definition);
        }

        /// <summary>
        /// Toes the dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.definition, Definition},
                {k.definitionName, Name},
                {k.quantity, Quantity},
                {k.attributeFlags,AttributeFlags.Flags},
                {k.categoryFlags, (long) CategoryFlags},
                {k.options, Options.ToGenxyString()},
                {k.hidden, _hidden},
                {k.health, Health},
                {k.descriptiontoken, _descriptionToken},
                {k.purchasable, Purchasable},
                {k.enablerExtension, EnablerExtensions.ToDictionary("e", e => e.Key.ToDictionary())},
                {k.config, Config.ToDictionary()},
                {k.tier,Tier.ToDictionary()}
            };
        }

        public override string ToString()
        {
            return $"Definition: {Definition}, DefinitionName: {Name}";
        }

        public double CalculateVolume(bool repackaged, int quantity = 1)
        {
            var perPieceVolume = Quantity == 1 ? Volume : Volume / Quantity;

            if (repackaged)
                perPieceVolume *= 0.5;

            return perPieceVolume * quantity;
        }

        public static double CalculateVolume(int definition, bool repackaged, int quantity = 1)
        {
            var ed = Get(definition);
            return ed.CalculateVolume(repackaged, quantity);
        }
    }
}