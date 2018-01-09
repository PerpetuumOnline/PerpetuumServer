using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationVoteSetTopic : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var voteID = request.Data.GetOrDefault<int>(k.voteID);
                var topic = request.Data.GetOrDefault<string>(k.topic);

                var corporation = character.GetPrivateCorporationOrThrow();
                corporation.SetVoteTopic(character, voteID, topic);
                
                scope.Complete();
            }
        }
    }
}