using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCancelEnterQueue : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            request.Zone.EnterQueueService.RemovePlayer(character);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}