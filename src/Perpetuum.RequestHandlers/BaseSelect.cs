using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    /// <summary>
    /// admin command to dock anywhere
    /// </summary>
    public class BaseSelect : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var baseEID = request.Data.GetOrDefault<long>(k.baseEID);

                character.CurrentDockingBaseEid = baseEID;
                character.IsDocked = true;
                character.ZoneId = null;
                character.ZonePosition = null;

                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> { { k.baseEID, baseEID } }).Send();
                
                scope.Complete();
            }
        }
    }
}