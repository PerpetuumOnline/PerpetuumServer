using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var corporationEids = request.Data.GetOrDefault<long[]>(k.eid);
            var result = CorporationData.GetAnyInfoDictionary(corporationEids);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}