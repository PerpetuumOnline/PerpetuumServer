using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;

namespace Perpetuum.Items.Templates
{
    public class ModuleTemplate : ItemTemplate<Module>
    {
        private readonly int _slot;

        [CanBeNull]
        private readonly ItemTemplate<Ammo> _ammo;

        private ModuleTemplate(int slot,ItemTemplate<Ammo> ammo) : base(1,false)
        {
            _slot = slot;
            _ammo = ammo;
        }

        public int Slot
        {
            get { return _slot; }
        }

        public override Dictionary<string,object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary[k.slot] = _slot;

            if (_ammo != null)
            {
                dictionary[k.ammoDefinition] = _ammo.EntityDefault.Definition;
                dictionary[k.ammoQuantity] = _ammo.Quantity;
            }

            return dictionary;
        }

        public static ModuleTemplate CreateFromDictionary(IDictionary<string, object> dictionary)
        {
            var definition = dictionary.GetValue<int>(k.definition);
            var slot = dictionary.GetValue<int>(k.slot);

            var ammoDefinition = dictionary.GetOrDefault(k.ammoDefinition, 0);
            if (ammoDefinition <= 0)
                return Create(definition, slot, null);

            var ammo = ItemTemplate<Ammo>.Create(ammoDefinition, dictionary.GetOrDefault(k.ammoQuantity, 0), false);
            return Create(definition, slot,ammo);
        }

        protected override bool OnValidate(Module module)
        {
            var activeModule = module as ActiveModule;
            if (activeModule != null)
            {
                if (activeModule.IsAmmoable)
                {
                    if (_ammo != null)
                    {
                        if (!_ammo.Validate())
                            return false;
                    }
                }
            }

            return base.OnValidate(module);
        }

        protected override void OnBuild(Module module)
        {
            var activeModule = module as ActiveModule;
            if (activeModule == null)
                return;

            if (!activeModule.IsAmmoable)
                return;

            if (_ammo != null)
            {
                var ammo = _ammo.Build();
                activeModule.SetAmmo(ammo);
            }

            base.OnBuild(module);
        }

        private static ModuleTemplate Create(int definition, int slot,ItemTemplate<Ammo> ammo)
        {
            return new ModuleTemplate(slot,ammo) { EntityDefault = EntityDefault.Get(definition) };
        }
    }
}