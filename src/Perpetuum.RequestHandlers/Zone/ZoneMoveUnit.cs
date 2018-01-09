using System;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting.Strategies;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneMoveUnit : IRequestHandler<IZoneRequest>
    {
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;

        public ZoneMoveUnit(ITeleportStrategyFactories teleportStrategyFactories)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var targetCharacter = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
            var targetPosition = new Position(request.Data.GetOrDefault<int>(k.x), request.Data.GetOrDefault<int>(k.y));
            var player = request.Zone.GetPlayerOrThrow(targetCharacter);
            var senderCharacter = request.Session.Character;

            var teleport = _teleportStrategyFactories.TeleportWithinZoneFactory();
            teleport.TargetPosition = targetPosition.Center;
            teleport.ApplyInvulnerable = true;
            teleport.TeleportDelay = TimeSpan.FromMilliseconds(500);
            teleport.DoTeleportAsync(player);

            Logger.Info($"unit was moved. issuer:{senderCharacter.Nick}  issuerid:{senderCharacter.Id} target character:{targetCharacter.Nick} targetid:{targetCharacter.Id}");
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}