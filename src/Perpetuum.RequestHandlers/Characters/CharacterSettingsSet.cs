using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSettingsSet : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var tHash = (Dictionary<string, object>)(request.Data[k.data]);

                var character = request.Session.Character;
                Db.Query().CommandText("characterSettingsSetString")
                    .SetParameter("@characterid", character.Id)
                    .SetParameter("@data", GenxyConverter.Serialize(tHash))
                    .ExecuteNonQuery();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}