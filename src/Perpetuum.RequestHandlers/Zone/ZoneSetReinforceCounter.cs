using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone
{
    /// <summary>
    /// Admin tool to set reinfoce counter
    /// </summary>
    public class ZoneSetReinforceCounter : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var value = request.Data.GetOrDefault<int>(k.value);

                var unit = request.Zone.GetUnit(eid);
                var pbsObject = unit as IPBSObject;
                if (pbsObject == null)
                    return;

                pbsObject.ReinforceHandler.ReinforceCounter = value;
                unit.DynamicProperties.Update(k.reinforceCounter, value);
                unit.Save();
                
                scope.Complete();
            }
        }
    }
}