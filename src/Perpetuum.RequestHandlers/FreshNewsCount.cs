using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class FreshNewsCount : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var language = request.Data.GetOrDefault<int>(k.language);
            var character = request.Session.Character;

            var newsCount = Db.Query().CommandText("freshNewsCount")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@language", language)
                .ExecuteScalar<int>();

            var result = new Dictionary<string, object> { { k.amount, newsCount } };
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}