using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSearch : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var nick = request.Data.GetOrDefault<string>(k.name);
            var pattern = request.Data.GetOrDefault<string>(k.name);

            pattern.Equals(string.Empty).ThrowIfTrue(ErrorCodes.SearchStringTooShort);
            pattern.Length.ThrowIfLess(2, ErrorCodes.SearchStringTooShort);

            pattern = $"%{pattern}%";

            var result = Db.Query().CommandText("select top(128) characterid from characters where nick like @pattern and active=1 order by nick")
                .SetParameter("@pattern", pattern)
                .Execute()
                .Select(record => record.GetValue<int>(0)).ToList();

            var exactId = Db.Query().CommandText("select characterid from characters where nick=@nick and active=1").SetParameter("@nick",nick).ExecuteScalar<int>();

            if (exactId != 0)
            {
                result.Remove(exactId);
                result.Insert(0, exactId);
            }

            var replyDict = new Dictionary<string, object>(1);

            if (result.Count > 0)
            {
                replyDict.Add(k.result, result.ToArray());
            }
            else
            {
                replyDict.Add(k.state, k.empty);
            }

            Message.Builder.FromRequest(request).WithData(replyDict).Send();
        }
    }
}