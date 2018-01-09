using Perpetuum.Host.Requests;
using Perpetuum.Players;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterListNpcDeath : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var from = request.Data.GetOrDefault<int>(k.from);
            var duration = request.Data.GetOrDefault<int>(k.duration);

            var character = request.Session.Character;
            var result = PlayerDeathLogger.GetHistory(character, from, duration);
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}