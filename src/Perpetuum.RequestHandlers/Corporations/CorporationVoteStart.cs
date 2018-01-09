using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationVoteStart : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var name = request.Data.GetOrDefault<string>(k.name);
                var topic = request.Data.GetOrDefault<string>(k.topic);
                var participation = request.Data.GetOrDefault<int>(k.participation);
                var consensusRate = request.Data.GetOrDefault<int>(k.consensusRate);

                var corporation = character.GetPrivateCorporationOrThrow();
                corporation.StartVote(character, name, topic, participation, consensusRate);
                
                scope.Complete();
            }
        }
    }
}