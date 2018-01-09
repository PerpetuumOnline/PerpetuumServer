using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class Dock : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var baseEid = request.Data.GetOrDefault<long>(k.baseEID);
                var character = request.Session.Character;
                var player = request.Zone.GetPlayer(character).ThrowIfNull(ErrorCodes.PlayerNotFound);
                player.CheckDockingConditionsAndThrow(baseEid);
                
                scope.Complete();
            }
        }
    }
}