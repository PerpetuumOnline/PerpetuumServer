using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationGetReputation : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public CorporationGetReputation(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporationEid = character.CorporationEid;

            DefaultCorporationDataCache.IsCorporationDefault(corporationEid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);

            var standingData = _standingHandler.GetReputationFor(corporationEid).ToDictionary("d", info =>
            {
                return new Dictionary<string, object>
                {
                    {k.sourceEID, info.sourceEID},
                    {k.standing, info.standing}
                };
            });

            var result = new Dictionary<string, object>
            {
                {k.targetEID, corporationEid},
                {k.standing, standingData}
            };

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}