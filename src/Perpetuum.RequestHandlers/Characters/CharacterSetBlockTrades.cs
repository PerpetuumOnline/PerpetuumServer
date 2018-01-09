using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSetBlockTrades : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var state = request.Data.GetOrDefault<int>(k.state).ToBool();
                var character = request.Session.Character;
                character.BlockTrades = state;

                var result = new Dictionary<string, object> { { k.state, state } };
                Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                
                scope.Complete();
            }
        }
    }
}