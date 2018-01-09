using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    /// <summary>
    /// Returns the distance contants
    /// </summary>
    public class GetDistances : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var distances = DistanceConstants.GetEnumDictionary();
            Message.Builder.FromRequest(request)
                .WithData(new Dictionary<string, object> { { k.distance, distances } })
                .Send();
        }
    }
}