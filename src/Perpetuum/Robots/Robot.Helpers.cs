using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Robots
{
    public class HybridRobotBuilder
    {
        private readonly IEntityServices _entityServices;

        public HybridRobotBuilder(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public Robot Build(long headEid,long chassisEid,long legEid,Character owner)
        {
            var chassis = RobotComponent.GetOrThrow(chassisEid);
            chassis.CheckOwnerCharacterAndCorporationAndThrowIfFailed(owner);

            var setup = RobotSetup.All.FirstOrDefault(s => s.Chassis == chassis.ED).ThrowIfNull(ErrorCodes.ServerError);

            // csinalunk robotot a definition alapjan a semmibe
            var robot = (Robot)_entityServices.Factory.CreateWithRandomEID(setup.HybridShell);
            robot.Owner = owner.Eid;

            // rogton belerakjuk a chassist
            robot.AddChild(chassis);

            // van feje?
            if (headEid > 0)
            {
                // akkor csinalunk fejet is neki, ezt is definition alapjan
                var head = RobotComponent.GetOrThrow(headEid);
                head.CheckOwnerCharacterAndCorporationAndThrowIfFailed(owner);
                robot.AddChild(head);
            }

            // laba?
            if (legEid > 0)
            {
                var leg = RobotComponent.GetOrThrow(legEid);
                leg.CheckOwnerCharacterAndCorporationAndThrowIfFailed(owner);
                robot.AddChild(leg);
            }

            //csinalunk kontenert
            var robotInventory = _entityServices.Factory.CreateWithRandomEID(setup.Container);
            robotInventory.Owner = owner.Eid;
            robot.AddChild(robotInventory);
            return robot;
        }

    }


    partial class Robot
    {
        public static readonly EntityDefault NoobBotEntityDefault = EntityDefault.GetByName(DefinitionNames.NOOB_BOT);

        [NotNull]
        public new static Robot GetOrThrow(long robotEid)
        {
            return (Robot)Item.GetOrThrow(robotEid);
        }
    }
}
