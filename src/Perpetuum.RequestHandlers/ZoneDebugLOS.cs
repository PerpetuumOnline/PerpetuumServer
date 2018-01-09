using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneDebugLOS : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var state = request.Data.GetOrDefault<int>(k.state) > 0;
            LineOfSight.Debug = state;
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}