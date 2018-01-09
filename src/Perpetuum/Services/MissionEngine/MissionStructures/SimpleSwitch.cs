using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{

    /// <summary>
    /// 
    /// Immediately enqueues the mission target
    /// Create success beam
    /// 
    /// </summary>
    public class SimpleSwitch : MissionSwitch
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void Use(Player player)
        {
            CanUseAndCheckError(player);

            player.MissionHandler.EnqueueMissionEventInfo(new SwitchEventInfo(player, this, CurrentPosition));

            //success beam kirajzolo
            CreateSuccessBeam(player);

        }
      
    }


}
