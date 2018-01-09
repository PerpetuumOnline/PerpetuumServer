using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionGetPauseTime : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var result = new Dictionary<string, object>
            {
                {k.from,Outpost.IntrusionPauseTime.Start}, 
                {k.to,Outpost.IntrusionPauseTime.End}
            };

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}