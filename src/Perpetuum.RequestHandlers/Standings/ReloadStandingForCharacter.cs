using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class ReloadStandingForCharacter : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public ReloadStandingForCharacter(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var characterId = request.Data.GetOrDefault<int>(k.characterID);
            var character = Character.Get(characterId).ThrowIfEqual(null, ErrorCodes.CharacterNotFound);

            _standingHandler.ReloadStandingForCharacter(character);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}