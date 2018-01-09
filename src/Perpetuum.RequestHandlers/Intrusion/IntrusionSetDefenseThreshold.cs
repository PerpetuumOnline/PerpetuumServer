using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionSetDefenseThreshold : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var siteEid = request.Data.GetOrDefault<long>(k.siteEID);
                var standingLimit = request.Data.GetOrDefault<double?>(k.standing);
                var character = request.Session.Character;

                var outpost = request.Zone.GetUnit(siteEid).ThrowIfNotType<Outpost>(ErrorCodes.IntrusionSiteNotFound);

                var siteInfo = outpost.GetIntrusionSiteInfo();

                if (siteInfo.DefenseStandingLimit == standingLimit)
                {
                    //nothing to do => exit
                    Message.Builder.FromRequest(request).WithOk().Send();
                    return;
                }

                var corporationEid = character.CorporationEid;

                //only owner corp controls
                siteInfo.Owner.ThrowIfNotEqual(corporationEid, ErrorCodes.AccessDenied);

                DefaultCorporationDataCache.IsCorporationDefault(corporationEid).ThrowIfTrue(ErrorCodes.PrivateCorporationAllowedOnly);

                var role = Corporation.GetRoleFromSql(character);
                role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                siteInfo.Stability.ThrowIfLess(Outpost.DefenseNodesStabilityLimit,ErrorCodes.StabilityTooLow);

                outpost.SetDefenseStandingLimit(standingLimit);
                
                scope.Complete();
            }
        }
    }
}
