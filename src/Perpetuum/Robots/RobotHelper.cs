using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Items;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Robots
{
    public class RobotHelper
    {
        private readonly UnitHelper _unitHelper;

        public RobotHelper(UnitHelper unitHelper)
        {
            _unitHelper = unitHelper;
        }

        public Robot GetOrLoadRobotForCharacter(long robotEid, Character character)
        {
            var robot = GetRobot(robotEid);
            if (robot == null)
            {
                robot = LoadRobotForCharacter(robotEid, character);
            }

            return robot;
        }

        public Robot GetRobot(long robotEid)
        {
            return _unitHelper.GetUnit<Robot>(robotEid);
        }

        public Robot LoadRobotForCharacter(long robotEid, Character character, bool checkOwner = false)
        {
            var robot = LoadRobot(robotEid);
            if (robot == null)
                return null;

            if (checkOwner)
            {
                robot.CheckOwnerOnlyCharacterAndThrowIfFailed(character);
            }

            robot.Initialize(character);
            return robot;
        }

        public Robot LoadRobot(long robotEid)
        {
            return (Robot) _unitHelper.LoadUnit(robotEid);
        }

        public Robot LoadRobotOrThrow(long robotEid)
        {
            return (Robot) _unitHelper.LoadUnitOrThrow(robotEid);
        }

        public bool IsSelected(Robot robot)
        {
            return Db.Query().CommandText("select count(*) from characters where activechassis=@robotEID")
                          .SetParameter("@robotEID", robot?.Eid)
                          .ExecuteScalar<int>() > 0;
        }
    }
}