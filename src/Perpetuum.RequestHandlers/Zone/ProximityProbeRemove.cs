using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.ProximityProbes;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ProximityProbeRemove : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);

                var probeBase = request.Zone.GetUnit(eid) as ProximityProbeBase;
                if (probeBase == null)
                    return;

                var character = request.Session.Character;
                probeBase.HasAccess(character).ThrowIfError();
                probeBase.Kill();
                
                scope.Complete();
            }
        }
    }
}