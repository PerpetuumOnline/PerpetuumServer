using System;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Modules
{
    public abstract class ArmorRepairerBaseModule : ActiveModule
    {
        protected readonly ModuleProperty armorRepairAmount;

        protected ArmorRepairerBaseModule(bool ranged) : base(ranged)
        {
            armorRepairAmount = new ModuleProperty(this, AggregateField.armor_repair_amount);
            armorRepairAmount.AddEffectModifier(AggregateField.effect_repair_amount_modifier);
            AddProperty(armorRepairAmount);
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.armor_repair_amount:
                case AggregateField.armor_repair_amount_modifier:
                case AggregateField.effect_repair_amount_modifier:
                    {
                        armorRepairAmount.Update();
                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        protected void OnRepair(Unit target, double amount)
        {
            if (amount <= 0.0) 
                return;

            var armor = target.Armor;
            target.Armor += amount;
            var total = Math.Abs(armor - target.Armor);

            var packet = new CombatLogPacket(CombatLogType.ArmorRepair, target, ParentRobot, this);
            packet.AppendDouble(amount);
            packet.AppendDouble(total);
            packet.Send(target, ParentRobot);
        }
    }

    public sealed class ArmorRepairModule : ArmorRepairerBaseModule
    {
        public ArmorRepairModule() : base(false)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnAction()
        {
            var amount = armorRepairAmount.Value;
            OnRepair(ParentRobot,amount);
            ParentRobot.SpreadAssistThreatToNpcs(ParentRobot,new Threat(ThreatType.Support,amount));

        }
    }
}