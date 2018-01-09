using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneForceDeconstruct : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var unit = request.Zone.GetUnit(eid);
                if (unit is IPBSObject o)
                {
                    o.ModifyConstructionLevel(900, true);
                }
                
                scope.Complete();
            }
        }
    }
}