using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.Modules
{
    public class NeuralyzerModule : ActiveModule
    {
        public NeuralyzerModule() : base(false)
        {
        }

        protected override void OnAction()
        {
            var myPlayer = (ParentRobot as Player).ThrowIfNull(ErrorCodes.ServerError);

            var players = Zone.Players.WithinRange(ParentRobot.CurrentPosition, DistanceConstants.NEURALYZER_RANGE);

            foreach (var player in players)
            {
                if (player.Eid == myPlayer.Eid)
                    continue;

                player.ResetLocks();
            }
        }
    }
}