using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Teleporting.Strategies;


namespace Perpetuum.RequestHandlers.Zone
{
    public class MovePlayer : IRequestHandler<IZoneRequest>
    {
        private readonly IZoneManager _zoneManager;
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private readonly ISessionManager _sessionManager;

        public MovePlayer(IZoneManager zoneManager, ITeleportStrategyFactories teleportStrategyFactories, ISessionManager sessionManager)
        {
            _zoneManager = zoneManager;
            _teleportStrategyFactories = teleportStrategyFactories;
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var characterid = request.Data.GetOrDefault<int>(k.characterID);
                var targetZoneId = request.Data.GetOrDefault<int>(k.zoneID);
                var x = request.Data.GetOrDefault<int>(k.x);
                var y = request.Data.GetOrDefault<int>(k.y);

                var TargetPosition = new Position(x, y);
                var charactersession = _sessionManager.GetByCharacter(characterid);
                var targetZone = _zoneManager.GetZone(targetZoneId);
                var player = request.Session.ZoneMgr.GetZone((int)charactersession.Character.ZoneId).GetPlayer(charactersession.Character.ActiveRobotEid);
                var s = _teleportStrategyFactories.TeleportToAnotherZoneFactory(targetZone);

                // cannot teleport players in training out of a training zone.
                // this allows GMs or devs to teleport themselves in and out of training zones.
                // player can still be moved around in the training zone.
                if (charactersession.Character.IsInTraining() && targetZone.GetType() != typeof(TrainingZone))
                {
                    Message.Builder.FromRequest(request).WithError(ErrorCodes.AccessDenied).Send();
                    return;
                }

                var position = new ClosestWalkablePositionFinder(targetZone, TargetPosition);
                if (!position.Find(out Position validPosition))
                {
                    return;
                }
                s.TargetPosition = validPosition;
                s.DoTeleport(player);

                Message.Builder.FromRequest(request).WithOk().Send();

                scope.Complete();
            }
        }
    }
}
