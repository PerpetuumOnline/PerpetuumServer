using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class GetStabilityBonusThresholds : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var data = Outpost.StabilityBonusThresholds.ToDictionary("s",i => i.ToDictionary());
            Message.Builder.FromRequest(request).WithData(data).WithEmpty().Send();
        }
    }
}
