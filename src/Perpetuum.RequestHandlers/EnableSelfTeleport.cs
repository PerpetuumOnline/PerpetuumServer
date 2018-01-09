using System;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Players;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class EnableSelfTeleport : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public EnableSelfTeleport(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
            var duration = TimeSpan.FromMinutes(request.Data.GetOrDefault<int>(k.durationMinutes));

            var player = _zoneManager.Zones.GetUnits().OfType<Player>().FirstOrDefault(p => p.Character == character);
            if (player == null)
                return;

            player.EnableSelfTeleport(duration);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}