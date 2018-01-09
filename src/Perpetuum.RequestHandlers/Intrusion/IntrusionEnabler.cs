using System.Collections.Generic;
using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class IntrusionEnabler : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public IntrusionEnabler(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var state = request.Data.GetOrDefault<int>(k.state);

            foreach (var zone in _zoneManager.Zones)
            {
                var outposts = zone.Units.OfType<Outpost>().ToArray();

                foreach (var outpost in outposts)
                {
                    outpost.Enabled = state.ToBool();
                }

                var siteEids = outposts.Select(s => s.Eid).ToArray();

                var result = new Dictionary<string,object>
                                 {
                                     {k.zoneID, zone.Id},
                                     {k.intrusionState, state},
                                     {k.sites,siteEids}
                                 };

                zone.SendMessageToPlayers(Message.Builder.SetCommand(Commands.IntrusionState).WithData(result));
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}