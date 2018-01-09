using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class GetIntrusionSiteInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var corporationEid = character.CorporationEid;

            if(DefaultCorporationDataCache.IsCorporationDefault(corporationEid))
            {
                Message.Builder.FromRequest(request).WithOk().Send();
                return;
            }

            var totalDict = Outpost.GetOwnershipPrivateInfo(corporationEid);
            
            var result = new Dictionary<string, object>
                             {
                                 {k.info, totalDict}
                             };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}
