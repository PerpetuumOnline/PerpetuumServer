using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class ForceFactionStandings : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public ForceFactionStandings(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var standing = request.Data.GetOrDefault<double>(k.standing).Clamp(-10, 10);

                var character = request.Session.Character;
                foreach (var allienceEid in DefaultCorporationDataCache.GetMegaCorporationEids())
                {
                    _standingHandler.SetStanding(allienceEid, character.Eid, standing);
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}