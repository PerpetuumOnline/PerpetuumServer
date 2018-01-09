using System.Collections.Generic;
using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.ProximityProbes;

namespace Perpetuum.RequestHandlers
{
    public class ProximityProbeList : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public ProximityProbeList(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporation = character.GetPrivateCorporationOrThrow();
            var role = corporation.GetMemberRole(character);

            var allProbes = ProximityProbeBase.IsAllProbesVisible(role);
            var probeEids = allProbes ? corporation.GetProximityProbeEids() : PBSRegisterHelper.PBSRegGetEidsByRegisteredCharacter(character);

            var probesDict = _zoneManager.Zones.GetUnits<ProximityProbeBase>()
                .Where(probeBase => probeEids.Contains(probeBase.Eid))
                .ToDictionary("c", p => p.ToDictionary());

            var reply = new Dictionary<string, object>
            {
                {k.probes, probesDict},
            };

            Message.Builder.FromRequest(request).WithData(reply).Send();
        }
    }
}