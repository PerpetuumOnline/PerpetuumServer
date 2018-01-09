using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionUpgradeFacility : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);
                var character = request.Session.Character;

                var facility = (ProductionFacility) Entity.Repository.LoadOrThrow(facilityEid);
                var outpost = facility.GetDockingBase().ThrowIfNotType<Outpost>(ErrorCodes.ItemNotFound);

                var siteInfo = outpost.GetIntrusionSiteInfo();
                siteInfo.Owner.ThrowIfNotEqual(character.CorporationEid, ErrorCodes.AccessDenied);

                var role = Corporation.GetRoleFromSql(character);
                role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                var currentProductionpoints = siteInfo.ProductionPoints.ThrowIfLessOrEqual(0, ErrorCodes.NotEnoughIntrusionProductionPoints);
                var facilityLevel = Outpost.GetFacilityLevelFromStack(facilityEid).ThrowIfGreaterOrEqual(Outpost.MAXIMUM_PRODUCTION_POINT_INDICES, ErrorCodes.IntrusionFacilityIsOnMaximumLevel);
                currentProductionpoints--;

                outpost.SetProductionPoints(currentProductionpoints);
                outpost.UpgradeFacility(facilityEid);
                outpost.InsertProductionLog(IntrusionEvents.upgradeFacility, facility.Definition, facilityLevel + 2, facilityLevel + 1, character, currentProductionpoints, currentProductionpoints + 1, siteInfo.Owner);

                Transaction.Current.OnCommited(() =>
                {
                    var resultingSiteInfo = outpost.GetIntrusionSiteInfo();
                    var result = new Dictionary<string, object>
                    {
                        {k.info, resultingSiteInfo.ToDictionary()},
                        {k.facility, facility.GetFacilityInfo(character)}
                    };

                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }
    }
}