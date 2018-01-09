using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelGetMutedCharacters : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var mutedCharacters = Db.Query().CommandText("select characterid from characters where globalmute = 1")
                .Execute()
                .Select(r => r.GetValue<int>(0))
                .ToArray();

            var result = new Dictionary<string, object>
            {
                { k.ID, mutedCharacters }
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}