using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class GetIntrusionMySitesLog : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var daysBack = request.Data.GetOrDefault<int>(k.offset);
            var character = request.Session.Character;
            var corporationEid = character.CorporationEid;

            if (DefaultCorporationDataCache.IsCorporationDefault(corporationEid))
            {
                Message.Builder.FromRequest(request).WithOk().Send();
                return;
            }

            var sapActivityDict = IntrusionHelper.GetMySitesLog(daysBack, corporationEid);
            Message.Builder.FromRequest(request)
                .WithData(new Dictionary<string, object>{{"intrusionPublicLog", sapActivityDict},})
                .Send();
        }
    }
}