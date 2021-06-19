using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.Robots
{
    public class RobotHead : RobotComponent
    {
        public RobotHead(IExtensionReader extensionReader) : base(RobotComponentType.Head,extensionReader)
        {
        }
    }

    public class RobotChassis : RobotComponent
    {
        public RobotChassis(IExtensionReader extensionReader) : base(RobotComponentType.Chassis,extensionReader)
        {
        }
    }

    public class RobotLeg : RobotComponent
    {
        public RobotLeg(IExtensionReader extensionReader) : base(RobotComponentType.Leg,extensionReader)
        {
        }
    }

    public abstract class RobotComponent : Item
    {
        private readonly RobotComponentType _type;
        private readonly IExtensionReader _extensionReader;

        protected RobotComponent(RobotComponentType type,IExtensionReader extensionReader)
        {
            _type = type;
            _extensionReader = extensionReader;
        }

        public override void Initialize()
        {
            InitModules();
            base.Initialize();
        }

        private Lazy<IEnumerable<Module>> _modules;
        private Lazy<IEnumerable<ActiveModule>> _activeModules;
        private void InitModules()
        {
            _modules = new Lazy<IEnumerable<Module>>(() => Children.OfType<Module>().ToArray());
            _activeModules = new Lazy<IEnumerable<ActiveModule>>(() => Modules.OfType<ActiveModule>().ToArray());
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public ExtensionBonus[] ExtensionBonuses
        {
            get { return _extensionReader.GetRobotComponentExtensionBonus(Definition); }
        }

        [CanBeNull]
        public Module GetModule(int slot)
        {
            return Modules.FirstOrDefault(m => m.Slot == slot);
        }

        public Robot ParentRobot
        {
            get { return (Robot)ParentEntity; }
        }

        public RobotComponentType Type
        {
            get { return _type; }
        }

        public string ComponentName
        {
            get { return _type.ToString().ToLower(); }
        }

        public IEnumerable<Module> Modules
        {
            get { return _modules.Value; }
        }

        public IEnumerable<ActiveModule> ActiveModules
        {
            get { return _activeModules.Value; }
        }

        public void Update(TimeSpan time)
        {
            foreach (var activeModule in ActiveModules)
            {
                activeModule.Update(time);
            }
        }

        private long GetSlotFlagMask(int slot)
        {
            return ED.Options.SlotFlags[slot - 1];
        }

        public int MaxSlots
        {
            get { return ED.Options.SlotFlags.Length; }
        }

        private bool IsValidModuleSlot(int slot)
        {
            return (slot > 0 && slot <= MaxSlots);
        }

        private bool IsUsedSlot(int slot)
        {
            foreach (var module in Modules)
            {
                if (module.Slot == slot)
                    return true;
            }

            return false;
        }

        public void MakeSlotFree(int slot, Container targetContainer)
        {
            var module = GetModule(slot);
            module?.Unequip(targetContainer);
        }

        public bool CheckUniqueModule(Module module)
        {
            if (ParentRobot == null)
                return true;

            CategoryFlags uniqueCategoryFlag;
            if (!module.ED.CategoryFlags.IsUniqueCategoryFlags(out uniqueCategoryFlag))
                return true;

            if (ParentRobot.FindModuleByCategoryFlag(uniqueCategoryFlag) != null)
                return false;

            return true;
        }

        public bool IsValidSlotTo(Module module, int slot)
        {
            if (!IsValidModuleSlot(slot))
                return false;

            var slotFlagMask = GetSlotFlagMask(slot);
            var moduleFlagMask = module.ModuleFlag;
            return (moduleFlagMask & slotFlagMask) == moduleFlagMask;
        }

        public ErrorCodes CanEquipModule(Module module, int slot)
        {
            if (IsUsedSlot(slot))
                return ErrorCodes.UsedSlot;

            if (!IsValidSlotTo(module, slot))
                return ErrorCodes.InvalidSlot;

            if (module.Quantity <= 0)
                return ErrorCodes.WTFErrorMedicalAttentionSuggested;

            if (module.IsDamaged)
                return ErrorCodes.ItemHasToBeRepaired;

            if (!CheckUniqueModule(module))
                return ErrorCodes.OnlyOnePerCategoryPerRobotAllowed;

            return ErrorCodes.NoError;
        }

        public void EquipModuleOrThrow(Module module, int slot)
        {
            CanEquipModule(module, slot).ThrowIfError();
            EquipModule(module, slot);
        }

        public void EquipModule(Module module, int slot)
        {
            if ( module == null )
                return;

            module.Owner = Owner;
            module.IsRepackaged = false;

            AddChild(module);
            module.Slot = slot;
        }

        private ErrorCodes CanChangeModule(int sourceSlot, int targetSlot)
        {
            var sourceModule = GetModule(sourceSlot);
            if (sourceModule != null && !IsValidSlotTo(sourceModule, targetSlot))
                return ErrorCodes.InvalidSlot;

            var targetModule = GetModule(targetSlot);
            if (targetModule != null && !IsValidSlotTo(targetModule,sourceSlot))
                return ErrorCodes.InvalidSlot;

            return ErrorCodes.NoError;
        }

        public void ChangeModuleOrThrow(int sourceSlot, int targetSlot)
        {
            CanChangeModule(sourceSlot, targetSlot).ThrowIfError();
            ChangeModule(sourceSlot,targetSlot);
        }

        private void ChangeModule(int sourceSlot, int targetSlot)
        {
            var sourceModule = GetModule(sourceSlot);
            var targetModule = GetModule(targetSlot);

            if (sourceModule != null)
                sourceModule.Slot = targetSlot;

            if (targetModule != null)
                targetModule.Slot = sourceSlot;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Add(k.modules, Modules.ToDictionary("m", m => m.ToDictionary()));
            return result;
        }

        public new static RobotComponent GetOrThrow(long componentEid)
        {
            return (RobotComponent)Repository.LoadOrThrow(componentEid);
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            var modifier = base.GetPropertyModifier(field);
            var modifyingModules = Modules.Where(m => !m.Properties.Any(p => p.Field == field));

            foreach (var module in modifyingModules)
            {
                var m = module.GetBasePropertyModifier(field);
                m.Modify(ref modifier);
            }

            return modifier;
        }

        public override void UpdateAllProperties()
        {
            foreach (var module in Modules)
            {
                module.UpdateAllProperties();
            }

            base.UpdateAllProperties();
        }

        public override void UpdateRelatedProperties(AggregateField field)
        {
            foreach (var module in Modules)
            {
                module.UpdateRelatedProperties(field);
            }

            base.UpdateRelatedProperties(field);
        }
    }
}
