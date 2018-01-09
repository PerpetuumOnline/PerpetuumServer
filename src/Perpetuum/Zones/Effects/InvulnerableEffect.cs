using System.Linq;
using Perpetuum.Modules;
using Perpetuum.Players;

namespace Perpetuum.Zones.Effects
{
    /// <summary>
    /// This effect provides invulnerability for the unit that it is applied on
    /// </summary>
    public class InvulnerableEffect : Effect
    {
        protected override void OnTick()
        {
            var player = Owner as Player;
            if( player == null )
                return;

            var isMoving = player.CurrentSpeed > 0;
            var hasAnyActiveModule = player.ActiveModules.Any(m => m.State.Type != ModuleStateType.Idle && m.State.Type != ModuleStateType.Disabled);
            if (isMoving || hasAnyActiveModule)
            {
                OnRemoved();
            }
       }
    }
}