using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class StandingList : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public StandingList(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var eid = request.Data.GetOrDefault<long>(k.eid).ThrowIfEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var allianceEID = character.AllianceEid;
            var corporationEID = character.CorporationEid;

            (eid != character.Eid && eid != corporationEID && eid != allianceEID).ThrowIfTrue(ErrorCodes.AccessDenied);

            var result = new Dictionary<string, object> { { k.sourceEID, eid } };

            var standings = _standingHandler.GetStandingsList(eid);
            if (standings != null)
                result.Add(k.standing, standings);

            if (eid == character.Eid)
                result.Add(k.characterEID, eid);

            if (eid == allianceEID)
                result.Add(k.allianceEID, eid);

            if (eid == corporationEID)
                result.Add(k.corporationEID, eid);

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}