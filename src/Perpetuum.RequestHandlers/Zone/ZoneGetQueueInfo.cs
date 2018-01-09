using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneGetQueueInfo : IRequestHandler<IZoneRequest>
    {
        public virtual void HandleRequest(IZoneRequest request)
        {
            var info = request.Zone.EnterQueueService.GetQueueInfoDictionary();
            Message.Builder.FromRequest(request).WithData(info).Send();
        }
    }
}