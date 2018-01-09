using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionSAPGetItemInfo : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var sapEid = request.Data.GetOrDefault<long>(k.target);

            var sap = request.Zone.GetUnit(sapEid) as SpecimenProcessingSAP;
            if (sap == null)
                throw new PerpetuumException(ErrorCodes.AttackPointWasNotFound);

            sap.SendProgressToPlayer(character);
        }
    }
}
