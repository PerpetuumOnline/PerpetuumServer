using System;
using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class StandingHistory : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public StandingHistory(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);

            var later = DateTime.Now - TimeSpan.FromDays(offsetInDays);
            var earlier = later - TimeSpan.FromDays(2);
            var logs = _standingHandler.GetStandingLogs(character, new DateTimeRange(earlier, later));
            var history = logs.ToDictionary("c", l => l.ToDictionary());

            var dictionary = new Dictionary<string, object>
            {
                { k.characterID, character.Id },
                { k.history,history }
            };

            Message.Builder.FromRequest(request).WithData(dictionary).WrapToResult().Send();
        }
    }
}