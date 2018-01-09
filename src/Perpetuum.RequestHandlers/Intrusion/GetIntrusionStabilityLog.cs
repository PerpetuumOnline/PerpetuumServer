using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class GetIntrusionStabilityLog : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public GetIntrusionStabilityLog(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var daysBack = request.Data.GetOrDefault<int>(k.day);
            var siteEid = request.Data.GetOrDefault<long>(k.eid);

            var outpost = _dockingBaseHelper.GetDockingBase(siteEid).ThrowIfNotType<Outpost>(ErrorCodes.ItemNotFound);
            var sapActivityDict = outpost.GetIntrusionStabilityLog(daysBack);

            Message.Builder.FromRequest(request)
                .WithData(new Dictionary<string, object>{{"stabilityLog", sapActivityDict},{k.siteEID, siteEid}})
                .Send();
        }
    }
}