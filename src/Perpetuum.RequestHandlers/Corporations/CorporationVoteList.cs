using System.Collections.Generic;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationVoteList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var corporation = character.GetPrivateCorporationOrThrow();

            var data = new Dictionary<string, object>
            {
                { k.vote, corporation.GetVotes().ToDictionary("v", v => v.ToDictionary()) },
                { k.open, corporation.GetOpenVoteIDs(character) }
            };

            Message.Builder.FromRequest(request).WithData(data).Send();
        }
    }
}