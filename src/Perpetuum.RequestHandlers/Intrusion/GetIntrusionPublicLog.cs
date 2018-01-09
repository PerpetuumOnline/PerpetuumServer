using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class GetIntrusionPublicLog : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var daysBack = request.Data.GetOrDefault<int>(k.offset);
            var sapActivityDict = Outpost.GetIntrusionStabilityPublicLog(daysBack);

            Message.Builder.FromRequest(request)
                .WithData(new Dictionary<string, object>{{"intrusionPublicLog", sapActivityDict},})
                .Send();
        }
    }
}