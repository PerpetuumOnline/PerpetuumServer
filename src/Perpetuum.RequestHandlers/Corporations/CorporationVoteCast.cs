using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationVoteCast : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var voteId = request.Data.GetOrDefault<int>(k.voteID);
                var voteChoice = (request.Data.GetOrDefault<int>(k.answer) == 1);

                var corporation = character.GetPrivateCorporationOrThrow();

                var vote = corporation.GetVote(voteId);
                if (vote == null)
                    throw new PerpetuumException(ErrorCodes.ItemNotFound);

                corporation.CastVote(vote, character, voteChoice);

                var result = new Dictionary<string, object>
                {
                    { k.vote, vote.ToDictionary() },
                    { k.details, corporation.GetVoteEntries(vote).ToDictionary("v",e => e.ToDictionary()) }
                };

                Message.Builder.SetCommand(Commands.CorporationVoteCast)
                    .WithData(result)
                    .ToCharacters(corporation.GetCharacterMembers())
                    .Send();
                
                scope.Complete();
            }
        }
    }
}