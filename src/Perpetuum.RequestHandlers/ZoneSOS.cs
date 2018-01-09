using Perpetuum.Host.Requests;
using Perpetuum.Players;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneSOS : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public ZoneSOS(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var player = _zoneManager.GetPlayer(character);
            if (player == null)
                return;

            var dockingBase = character.GetCurrentDockingBase();
            dockingBase.DockIn(character, Player.NormalUndockDelay, ZoneExitType.Docked);

            Message.Builder.FromRequest(request).WithOk().Send();
            Message.Builder.FromRequest(request).WithError(ErrorCodes.SOSStarted).Send();
        }
    }
}