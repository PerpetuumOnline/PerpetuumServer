using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterIsOnline : IRequestHandler
    {
        private readonly ISessionManager _sessionManager;

        public CharacterIsOnline(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var characters = request.Data.GetOrDefault<int[]>(k.characterID).ToCharacter();
            var onlineCharacters = _sessionManager.SelectedCharacters.Intersect(characters).GetCharacterIDs().ToArray();

            if (onlineCharacters.Length > 0)
            {
                var dictionary = new Dictionary<string, object> { { k.result, onlineCharacters } };
                Message.Builder.FromRequest(request)
                    .WithData(dictionary)
                    .Send();
            }
            else
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
            }
        }
    }
}