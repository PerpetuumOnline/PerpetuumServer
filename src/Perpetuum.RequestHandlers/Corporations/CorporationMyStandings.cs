using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationMyStandings : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public CorporationMyStandings(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporationEid = character.CorporationEid;

            DefaultCorporationDataCache.IsCorporationDefault(corporationEid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);
            var result = new Dictionary<string, object>();

            var standingsData = _standingHandler.GetStandingsList(corporationEid);
            if (standingsData != null)
            {
                result.Add(k.standing, standingsData);
                result.Add(k.corporationEID, corporationEid);
                Message.Builder.SetCommand(Commands.StandingList).WithData(result).ToClient(request.Session).Send();
            }
            else
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
            }
        }
    }
}