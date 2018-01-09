using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSSetStandingLimit : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.eid);
                var character = request.Session.Character;
                var standingLimit = request.Data.GetOrDefault<double?>(k.standing);

                var sourceUnit = request.Zone.GetUnitOrThrow(sourceEid);

                if (sourceUnit is IPBSObject sourceNode)
                {
                    //if PBS check the PBS way

                    sourceNode.CheckAccessAndThrowIfFailed(character);
                    sourceNode.IsFullyConstructed().ThrowIfFalse(ErrorCodes.ObjectNotFullyConstructed);

                    Transaction.Current.OnCommited(() => sourceNode.SendNodeUpdate());
                }
                else
                {
                    Corporation.GetCorporationEidAndRoleFromSql(character, out long corporationEid, out CorporationRole role);

                    (corporationEid == sourceUnit.Owner).ThrowIfFalse(ErrorCodes.AccessDenied);

                    role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.editPBS).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
                }

                if (sourceUnit is IStandingController controller)
                {
                    controller.StandingEnabled = standingLimit != null;
                }

                ((IHaveStandingLimit)sourceUnit).StandingLimit = standingLimit ?? 0.0;

                sourceUnit.Save();

                Message.Builder.FromRequest(request).WithData(sourceUnit.ToDictionary()).Send();
                
                scope.Complete();
            }
        }
    }
}