using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.ProximityProbes;

namespace Perpetuum.RequestHandlers
{
    public class ProximityProbeGetRegistrationInfo : IRequestHandler
    {
        private readonly UnitHelper _unitHelper;

        public ProximityProbeGetRegistrationInfo(UnitHelper unitHelper)
        {
            _unitHelper = unitHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var probeEid = request.Data.GetOrDefault<long>(k.eid);
            var character = request.Session.Character;

            var probe = _unitHelper.GetUnitOrThrow<ProximityProbeBase>(probeEid);
            probe.HasAccess(character).ThrowIfError();

            var result = probe.GetProbeRegistrationInfo();
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}