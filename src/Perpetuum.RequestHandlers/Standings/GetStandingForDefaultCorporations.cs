using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class GetStandingForDefaultCorporations : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public GetStandingForDefaultCorporations(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = _standingHandler.GetStandingForDefaultCorporations(character) ?? new Dictionary<string, object>();
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}