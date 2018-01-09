using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.GenXY;
using Perpetuum.Modules;

namespace Perpetuum.Robots.Fitting
{
    public class FittingPreset
    {
        private readonly ModuleInfo[] _modules;

        private FittingPreset(string name,EntityDefault robot,IEnumerable<ModuleInfo> modules)
        {
            _modules = modules.ToArray();
            Name = name;
            Robot = robot;
        }

        public int Id { get; set; }
        public long Owner { get; set; }

        public string Name { get; set; }
        public EntityDefault Robot { get; set; }

        public IEnumerable<ModuleInfo> Modules
        {
            get { return _modules; }
        }

        public GenxyString ToGenxyString()
        {
            var dictionary = new Dictionary<string, object>
            {
                {k.name, Name}, 
                {k.robot, Robot.Definition}, 
                {k.modules, _modules.ToDictionary("m", m => m.ToDictionary())}
            };

            return GenxyConverter.Serialize(dictionary);
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.ID,Id},
                {k.owner,Owner},
                {k.name, Name}, 
                {k.robot, Robot.Definition}, 
                {k.modules, _modules.ToDictionary("m", m => m.ToDictionary())}
            };
        }

        public static FittingPreset CreateFrom(GenxyString preset)
        {
            var dictionary = preset.ToDictionary();

            var name = dictionary.GetOrDefault<string>(k.name);
            var robot = EntityDefault.Get(dictionary.GetOrDefault<int>(k.robot));
            var modules = dictionary.GetOrDefault<IDictionary<string, object>>(k.modules,() => new Dictionary<string, object>()).Select(kvp => ModuleInfo.CreateFrom((IDictionary<string, object>) kvp.Value));

            return new FittingPreset(name,robot,modules);
        }

        public static FittingPreset CreateFrom(Robot robot)
        {
            var name = robot.Name;
            var modules = robot.Modules.Select(ModuleInfo.CreateFrom);
            return new FittingPreset(name,robot.ED,modules);
        }

        public class ModuleInfo
        {
            public ModuleInfo(RobotComponentType component,int slot,EntityDefault module,EntityDefault ammo)
            {
                Component = component;
                Slot = slot;
                Module = module;
                Ammo = ammo;
            }

            public RobotComponentType Component { get; set; }
            public int Slot { get; set; }
            public EntityDefault Module { get; set; }
            public EntityDefault Ammo { get; set; }

            public IDictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    {k.component, (int) Component}, 
                    {k.slot, Slot}, 
                    {k.module, Module.Definition}, 
                    {k.ammo, Ammo.Definition}
                };
            }

            public static ModuleInfo CreateFrom(IDictionary<string, object> dictionary)
            {
                var component = (RobotComponentType)dictionary.GetOrDefault<int>(k.component);
                var slot = dictionary.GetOrDefault<int>(k.slot);
                var module = EntityDefault.Get(dictionary.GetOrDefault<int>(k.module));
                var ammo = EntityDefault.Get(dictionary.GetOrDefault<int>(k.ammo));

                return new ModuleInfo(component,slot,module,ammo);
            }

            public static ModuleInfo CreateFrom(Module module)
            {
                var ammoEntityDefault = EntityDefault.None;

                var activeModule = module as ActiveModule;
                var ammo = activeModule?.GetAmmo();
                if (ammo != null)
                    ammoEntityDefault = ammo.ED;

                return new ModuleInfo(module.ParentComponent.Type,module.Slot,module.ED,ammoEntityDefault);
            }
        }
    }
}