using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneListPresences : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var result = request.Zone.PresenceManager.GetPresences().ToDictionary("p", p => p.ToDictionary(true));
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}