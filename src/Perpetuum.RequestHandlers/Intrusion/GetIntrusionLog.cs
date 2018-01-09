using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class GetIntrusionLog : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public GetIntrusionLog(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);
            var siteEid = request.Data.GetOrDefault<long>(k.eid);

            var outpost = _dockingBaseHelper.GetDockingBase(siteEid).ThrowIfNotType<Outpost>(ErrorCodes.ItemNotFound);
            var siteInfo = outpost.GetIntrusionSiteInfo();

            var character = request.Session.Character;
            var corporationEid = character.CorporationEid;
            siteInfo.Owner.ThrowIfNotEqual(corporationEid, ErrorCodes.AccessDenied);

            var sapActivityDict = outpost.GetIntrusionCorporationLog(offsetInDays, corporationEid);
            var dockingRightsDict = outpost.GetDockingRightsLog(offsetInDays, corporationEid);
            var effectsDict = outpost.GetIntrusionEffectLog(offsetInDays, corporationEid);
            var messageLogDict = outpost.GetMessageChangeLog(offsetInDays, corporationEid);
            var productionLog = outpost.GetIntrusionProductionLog(offsetInDays, corporationEid);

            Message.Builder.FromRequest(request).WithData(new Dictionary<string, object>
            {
                {"activityLog", sapActivityDict},
                {"dockingRightsLog", dockingRightsDict},
                {"effectsLog", effectsDict},
                {"messageLog", messageLogDict},
                {"production", productionLog},
                {k.siteEID, siteEid}
            }).Send();
        }
    }
}