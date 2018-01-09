using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSSetReinforceOffset : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var offset = request.Data.GetOrDefault<int>(k.offset);

                offset = offset.Clamp(0, 23);

                var pbsObject = (request.Zone.GetUnitOrThrow(eid) as PBSDockingBase).ThrowIfNull(ErrorCodes.OnlyPBSDockingBaseAllowed);
                pbsObject.CheckAccessAndThrowIfFailed(character);

                Corporation.GetRoleFromSql(character).IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                pbsObject.ReinforceHandler.SetReinforceOffset(character, offset);
                pbsObject.Save();

                Transaction.Current.OnCommited(() => pbsObject.SendNodeUpdate());
                scope.Complete();
            }
        }
    }
}