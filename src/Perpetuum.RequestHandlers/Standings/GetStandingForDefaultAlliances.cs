using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class GetStandingForDefaultAlliances : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public GetStandingForDefaultAlliances(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = _standingHandler.GetStandingForDefaultAlliances(character) ?? new Dictionary<string, object>();
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}