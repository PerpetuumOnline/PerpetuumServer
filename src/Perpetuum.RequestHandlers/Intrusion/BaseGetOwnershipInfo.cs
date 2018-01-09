using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class BaseGetOwnershipInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request)
                .WithData(new Dictionary<string, object> { { k.data, Outpost.GetOwnershipInfo() } })
                .Send();
        }
    }
}