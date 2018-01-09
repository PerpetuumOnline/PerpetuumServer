using System;
using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionSetPauseTime : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var from = request.Data.GetOrDefault<DateTime>(k.@from);
            var to = request.Data.GetOrDefault<DateTime>(k.to);

            Outpost.IntrusionPauseTime = new DateTimeRange(@from, to);

            var result = new Dictionary<string, object>
            {
                {k.from,from}, 
                {k.to,to}
            };

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}