using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSetHomeBase : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var dockingBase = character.GetCurrentDockingBase();
                dockingBase.IsDockingAllowed(character).ThrowIfError();
                character.HomeBaseEid = dockingBase.Eid;

                var dictionary = new Dictionary<string, object>
                {
                    {k.characterID, character.Id},
                    {k.homeBaseEID, dockingBase.Eid},
                };

                Message.Builder.FromRequest(request).WithData(dictionary).Send();
                
                scope.Complete();
            }
        }
    }
}