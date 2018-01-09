using Perpetuum.EntityFramework;
using Perpetuum.Units;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{
    public class MissionStructureNameGenerator : IEntityVisitor<AlarmSwitch>,IEntityVisitor<SimpleSwitch>,IEntityVisitor<ItemSupply>,IEntityVisitor<Kiosk>
    {
        public static void Generate(Unit unit)
        {
            var generator = new MissionStructureNameGenerator();
            unit.AcceptVisitor(generator);
        }

        public void Visit(AlarmSwitch alarmSwitch)
        {
            alarmSwitch.Name = "missionstructure_switch";
        }

        public void Visit(SimpleSwitch simpleSwitch)
        {
            simpleSwitch.Name = "missionstructure_switch";
        }

        public void Visit(ItemSupply itemSupply)
        {
            itemSupply.Name = "missionstructure_itemsupply";
        }

        public void Visit(Kiosk kiosk)
        {
            kiosk.Name = "missionstructure_itemsubmit";
        }
    }
}
