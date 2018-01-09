using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting.Strategies;

namespace Perpetuum.RequestHandlers.Zone
{
    public class JumpAnywhere : IRequestHandler<IZoneRequest>
    {
        private readonly IZoneManager _zoneManager;
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;

        public JumpAnywhere(IZoneManager zoneManager, ITeleportStrategyFactories teleportStrategyFactories)
        {
            _zoneManager = zoneManager;
            _teleportStrategyFactories = teleportStrategyFactories;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var targetZoneId = request.Data.GetOrDefault<int>(k.zoneID);
                var x = request.Data.GetOrDefault<int>(k.x);
                var y = request.Data.GetOrDefault<int>(k.y);

                var targetZone = _zoneManager.GetZone(targetZoneId);
                var player = request.Zone.GetPlayer(request.Session.Character);
                var s = _teleportStrategyFactories.TeleportToAnotherZoneFactory(targetZone);
                s.TargetPosition = new Position(x, y).Clamp(targetZone.Size);
                s.DoTeleport(player);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}