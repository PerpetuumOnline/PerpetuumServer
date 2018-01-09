using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSettingsGet : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var dataStr = Db.Query().CommandText("select settingsstring from charactersettings where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteScalar<string>();

            var result = GenxyConverter.Deserialize(dataStr);

            if (result.Count == 0)
            {
                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object>(1) { { k.state, k.empty } }).Send();
            }
            else
            {
                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object>(1) { { k.result, result } }).Send();
            }
        }
    }
}