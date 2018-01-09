using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSSetTerritoryVisibility : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var eid = request.Data.GetOrDefault<long>(k.eid);

                var netVis = request.Data.GetOrDefault(k.territoryMapVisible, -1);
                var baseVis = request.Data.GetOrDefault(k.mapVisible, -1);

                var corporation = character.GetPrivateCorporationOrThrow();

                corporation.GetMemberRole(character).IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                var pbsDockingBase = (PBSDockingBase)request.Zone.GetUnitOrThrow(eid);
                pbsDockingBase.CheckAccessAndThrowIfFailed(character);

                if (netVis >= 0)
                {
                    pbsDockingBase.NetworkMapVisibility = (PBSDockingBaseVisibility)netVis;    
                }

                if (baseVis >= 0)
                {
                    pbsDockingBase.DockingBaseMapVisibility = (PBSDockingBaseVisibility) baseVis;
                }

                pbsDockingBase.Save();
                Transaction.Current.OnCommited(() => pbsDockingBase.SendNodeUpdate());
                scope.Complete();
            }
        }
    }
}