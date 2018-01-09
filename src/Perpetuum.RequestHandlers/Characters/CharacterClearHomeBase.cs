using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterClearHomeBase : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.HomeBaseEid = null;
                var data = new Dictionary<string, object>
                {
                    { k.characterID, character.Id }
                };

                Message.Builder.FromRequest(request).WithData(data).Send();
                
                scope.Complete();
            }
        }
    }
}