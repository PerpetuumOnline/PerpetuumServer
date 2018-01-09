using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.MissionRequests
{
    public class RemoveMissionStructure : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var unit = request.Zone.GetUnitOrThrow(eid);
                unit.ED.CategoryFlags.IsCategory(CategoryFlags.cf_mission_structures).ThrowIfFalse(ErrorCodes.DefinitionNotSupported);

                request.Zone.UnitService.RemoveDefaultUnit(unit,true);
                MissionTarget.DeleteByStrucureEid(eid);
                unit.RemoveFromZone();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}
