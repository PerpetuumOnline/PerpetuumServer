using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationNameHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var corporationEid = request.Data.GetOrDefault<long>(k.corporationEID);

            var result =
            Corporation.ListPreviousAliasesToDictionary(corporationEid);

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}
