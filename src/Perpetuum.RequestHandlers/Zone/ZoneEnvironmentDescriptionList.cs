using System.Collections.Generic;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneEnvironmentDescriptionList : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var result = new Dictionary<string, object>
            {
                {k.definition,request.Zone.Environment.ListEnvironmentDescriptions()}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}