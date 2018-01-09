using Perpetuum.EntityFramework;
using Perpetuum.Robots;

namespace Perpetuum.Items.Templates
{
    public class RobotComponentTemplate<T> : ItemTemplate<T> where T:RobotComponent
    {
        private readonly ModuleTemplate[] _moduleTemplates;

        private RobotComponentTemplate(ModuleTemplate[] moduleTemplates = null) : base(1,false)
        {
            _moduleTemplates = moduleTemplates ?? new ModuleTemplate[0];
        }

        public ModuleTemplate[] Modules
        {
            get { return _moduleTemplates; }
        }

        protected override bool OnValidate(T component)
        {
            foreach (var moduleTemplate in _moduleTemplates)
            {
                if (!moduleTemplate.Validate())
                    continue;

                var module = moduleTemplate.Build();

                if (!component.IsValidSlotTo(module, moduleTemplate.Slot))
                    return false;

                if (!component.CheckUniqueModule(module))
                    return false;
            }

            return base.OnValidate(component);
        }

        protected override void OnBuild(T component)
        {
            foreach (var moduleTemplate in _moduleTemplates)
            {
                var module = moduleTemplate.Build();
                component.EquipModule(module, moduleTemplate.Slot);
            }

            base.OnBuild(component);
        }

        public static RobotComponentTemplate<T> Create(int definition, ModuleTemplate[] moduleTemplates)
        {
            return new RobotComponentTemplate<T>(moduleTemplates) { EntityDefault = EntityDefault.Get(definition) };
        }
    }
}