using System.Collections.Generic;
using System.Linq;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Zones;

namespace Perpetuum.Modules
{
    public class Module : Item
    {
        private readonly ItemProperty _powerGridUsage;
        private readonly ItemProperty _cpuUsage;

        public Module()
        {
            _powerGridUsage = new ModuleProperty(this, AggregateField.powergrid_usage);
            AddProperty(_powerGridUsage);
            _cpuUsage = new ModuleProperty(this,AggregateField.cpu_usage);
            AddProperty(_cpuUsage);
        }

        public ILookup<AggregateField,AggregateField> PropertyModifiers { get; set; }

        public double PowerGridUsage
        {
            get { return _powerGridUsage.Value; }
        }

        public double CpuUsage
        {
            get { return _cpuUsage.Value; }
        }

        public IEnumerable<AggregateField> GetPropertyModifiers()
        {
            return PropertyModifiers.SelectMany(g => g);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        [CanBeNull]
        public RobotComponent ParentComponent => GetOrLoadParentEntity() as RobotComponent;

        [CanBeNull]
        public Robot ParentRobot => ParentComponent?.ParentRobot;

        [CanBeNull]
        protected IZone Zone => ParentRobot?.Zone;

        public bool ParentIsPlayer()
        {
            return ParentRobot is Player;
        }

        public int Slot
        {
            get { return DynamicProperties.GetOrDefault<int>(k.slot); }
            set { DynamicProperties.Update(k.slot,value); }
        }

        public long ModuleFlag
        {
            get { return ED.Options.ModuleFlag; }
        }

        public virtual void Unequip(Container container)
        {
            if (!IsRepackaged)
                this.Pack();

            container.AddItem(this, true);
            Slot = 0;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();

            result.Add(k.slot, Slot);
            result.Add(k.state, (byte) ModuleStateType.Idle);

            return result;
        }

        public bool IsPassive
        {
            get { return ED.AttributeFlags.PassiveModule; }
        }

        protected virtual void OnUpdateProperty(AggregateField field)
        {
        }

        public virtual void UpdateProperty(AggregateField field)
        {
            OnUpdateProperty(field);
        }

        public Packet BuildModuleInfoPacket()
        {
            var packet = new Packet(ZoneCommand.ModuleInfoResult);

            packet.AppendByte((byte)ParentComponent.Type);
            packet.AppendByte((byte) Slot);

            var properties = Properties.ToList();

            packet.AppendByte((byte)properties.Count);

            foreach (var property in properties)
            {
                property.AppendToPacket(packet);
            }

            return packet;
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            var modifier = base.GetPropertyModifier(field);
            ApplyRobotPropertyModifiers(ref modifier);
            return modifier;
        }

        public void ApplyRobotPropertyModifiers(ref ItemPropertyModifier modifier)
        {
            var modifiers = PropertyModifiers.GetOrEmpty(modifier.Field);

            foreach (var m in modifiers)
            {
                ParentRobot?.GetPropertyModifier(m).Modify(ref modifier);
            }
        }
    }
}