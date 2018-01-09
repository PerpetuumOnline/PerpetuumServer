using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class TeleportToZoneObject : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var player = request.Zone.GetPlayerOrThrow(character);

            player.States.Combat.ThrowIfTrue(ErrorCodes.OperationNotAllowedInCombat);
            player.HasPvpEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);
            player.HasSelfTeleportEnablerEffect.ThrowIfFalse(ErrorCodes.SelfTeleportEnablerMissing);

            var targetEid = request.Data.GetOrDefault<long>(k.target);
            var targetUnit = request.Zone.GetUnitOrThrow(targetEid);

            var task = player.TeleportToPositionAsync(targetUnit.CurrentPosition, request.Zone.Configuration.IsBeta, false);
            task?.ContinueWith(t =>
            {
                player.RemoveSelfTeleportEnablerEffect();
            });
        }
    }
}