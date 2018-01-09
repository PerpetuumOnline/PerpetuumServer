using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneEntityChangeState : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var targetEid = request.Data.GetOrDefault<long>(k.targetEID);
            var entityState = request.Data.GetOrDefault<int>(k.bit);
            var bitState = request.Data.GetOrDefault<int>(k.state);

            var unit = request.Zone.GetUnitOrThrow(targetEid);
            unit.States.SetFlag((UnitStateFlags)(((ulong)1) << entityState), bitState > 0);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}