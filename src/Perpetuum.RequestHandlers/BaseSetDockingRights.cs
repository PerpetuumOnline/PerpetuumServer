using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers
{
    public class BaseSetDockingRights : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public BaseSetDockingRights(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var lockBase = request.Data.GetOrDefault<int>(k.locked) == 1;
                double? standingLimit = request.Data.GetOrDefault<double>(k.standing);
                var baseEid = request.Data.GetOrDefault<long>(k.baseEID);

                var eventType = IntrusionEvents.dockingRightsSet;
                if (!lockBase)
                {
                    standingLimit = null; //opening the outpost for everyone
                    eventType = IntrusionEvents.dockingRightsClear;
                }

                if (!(_dockingBaseHelper.GetDockingBase(baseEid) is Outpost outpost))
                    throw new PerpetuumException(ErrorCodes.OperationAllowedOnlyOnIntrusionSites);

                var siteInfo = outpost.GetIntrusionSiteInfo();
                if (siteInfo.DockingStandingLimit == standingLimit)
                {
                    //nothing to do => exit
                    Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> { { k.baseEID,outpost.Eid } }).Send();
                    return;
                }

                var owner = siteInfo.Owner.ThrowIfNull(ErrorCodes.AccessDenied);

                var corporationEid = character.CorporationEid;

                //only owner corp controls the docking rights
                corporationEid.ThrowIfNotEqual((long) owner, ErrorCodes.AccessDenied);

                DefaultCorporationDataCache.IsCorporationDefault(corporationEid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);

                var role = Corporation.GetRoleFromSql(character);
                role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                var stabilityLimit = Outpost.GetDockingRightsStabilityLimit();

                siteInfo.Stability.ThrowIfLess(stabilityLimit,ErrorCodes.StabilityTooLow);

                var dockingControlLimit = siteInfo.DockingControlLimit;
                if (dockingControlLimit != null)
                {
                    DateTime.Now.ThrowIfLess((DateTime)dockingControlLimit,ErrorCodes.DockingRightsChangeCooldownInProgress);
                }

                outpost.SetDockingControlDetails(standingLimit, !lockBase);
                outpost.InsertDockingRightsLog(character, standingLimit, corporationEid, eventType);

                Transaction.Current.OnCommited(() => outpost.SendSiteInfoToOnlineCharacters());

                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> { { k.baseEID, baseEid } }).Send();
                
                scope.Complete();
            }
        }
    }
}