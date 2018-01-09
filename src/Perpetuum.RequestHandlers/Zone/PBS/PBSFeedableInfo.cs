using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSFeedableInfo : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var eid = request.Data.GetOrDefault<long>(k.eid);

            var info = new Dictionary<string, object>();

            var feedableUnit = request.Zone.GetUnitOrThrow(eid);
            var feedable = (feedableUnit as IPBSFeedable).ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            info = feedableUnit.ToDictionary();

            if (info.Count == 0)
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
            }
            else
            {
                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> {{k.info, info}}).Send();
            }
        }
    }
}