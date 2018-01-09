using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionSAPSubmitItem : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sapEid = request.Data.GetOrDefault<long>(k.target);
                var itemEid = request.Data.GetOrDefault<long>(k.eid);

                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);
                var sap = (SpecimenProcessingSAP) request.Zone.GetUnit(sapEid).ThrowIfNull(ErrorCodes.AttackPointWasNotFound);
                sap.SubmitItem(player, itemEid);
                
                scope.Complete();
            }
        }
    }
}
