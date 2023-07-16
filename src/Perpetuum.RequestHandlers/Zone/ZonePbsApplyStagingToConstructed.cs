using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;
using System.Linq;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZonePbsApplyStagingToConstructed : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var pbsObjectsOnZone = request.Zone.Units.Where(u => u is IPBSObject).ToList();

                foreach (var unit in pbsObjectsOnZone)
                {
                    request.Zone.CleanEnvironmentByUnit(unit);
                    request.Zone.DrawEnvironmentByUnitFromStaging(unit);
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                scope.Complete();
            }
        }
    }
}
