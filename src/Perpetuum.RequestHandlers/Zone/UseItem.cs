using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class UseItem : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var eid = request.Data.GetOrDefault<long>(k.eid);

            var unit = request.Zone.GetUnitOrThrow(eid);

            var character = request.Session.Character;
            var v = new UseItemVisitor(request.Zone, character);
            unit.AcceptVisitor(v);
        }
    }
}
