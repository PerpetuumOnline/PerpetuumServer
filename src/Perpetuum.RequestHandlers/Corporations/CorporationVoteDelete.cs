using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationVoteDelete : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var voteID = request.Data.GetOrDefault<int>(k.voteID);

                var corporation = character.GetPrivateCorporationOrThrow();
                corporation.DeleteVote(character, voteID);
                
                scope.Complete();
            }
        }
    }
}