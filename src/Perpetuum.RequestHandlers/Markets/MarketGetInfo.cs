using System;
using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketGetInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var eids = request.Data.GetOrDefault<long[]>(k.eid);

            var result = new Dictionary<string, object>();
            var counter = 0;
            foreach (var marketEid in eids)
            {
                try
                {
                    var market = Market.GetOrThrow(marketEid);
                    result.Add("m" + counter++, market.ToDictionary());
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
            
            Message.Builder.FromRequest(request).WithData(result).Send();

        }
    }

}
