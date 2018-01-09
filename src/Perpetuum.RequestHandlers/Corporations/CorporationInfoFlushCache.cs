using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationInfoFlushCache : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            CorporationData.FlushCache();
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}
