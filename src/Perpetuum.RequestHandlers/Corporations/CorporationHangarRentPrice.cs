using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarRentPrice : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var hangarEid = request.Data.GetOrDefault<long>(k.eid);

            var hangarStorage = (PublicCorporationHangarStorage)Container.GetOrThrow(hangarEid);
            var rentInfo = hangarStorage.GetCorporationHangarRentInfo();

            var result = new Dictionary<string, object>
            {
                { k.price, rentInfo.price },
                { k.rentPeriod, (int)rentInfo.period.TotalDays },
                { k.eid, hangarEid }
            };

            Message.Builder.FromRequest(request)
                .WithData(result)
                .Send();
        }
    }
}