using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules;
using Perpetuum.Robots;

namespace Perpetuum.Items.Ammos
{
    public class Ammo : Item
    {
        public double BulletTime => ED.Options.BulletTime;

        public ItemPropertyModifier OptimalRangePropertyModifier => GetPropertyModifier(AggregateField.optimal_range);

        public ItemPropertyModifier FalloffRangePropertyModifier => GetPropertyModifier(AggregateField.falloff);

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public virtual void ModifyOptimalRange(ref ItemPropertyModifier property) { }

        public void ModifyFalloff(ref ItemPropertyModifier falloff)
        {
            var falloffMod = GetPropertyModifier(AggregateField.falloff_modifier);
            falloffMod.Modify(ref falloff);
        }

        public virtual void ModifyCycleTime(ref ItemPropertyModifier modifier) { }

        [CanBeNull]
        public Module GetParentModule()
        {
            return GetOrLoadParentEntity() as Module;
        }

        [CanBeNull]
        public Robot GetParentRobot()
        {
            return GetParentModule()?.ParentRobot;
        }

    }
}