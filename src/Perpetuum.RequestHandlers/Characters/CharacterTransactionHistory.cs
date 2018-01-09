using System.Collections.Generic;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterTransactionHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);
            var dictionary = new Dictionary<string, object>
            {
                { k.characterID, character.Id }, 
                { k.history, character.GetTransactionHistory(offsetInDays) }
            };

            Message.Builder.FromRequest(request)
                .WithData(dictionary)
                .WrapToResult()
                .Send();
        }
    }
}