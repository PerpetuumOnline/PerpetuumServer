using System.Collections.Generic;
using System.Linq;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationGetDelegates : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var corporationEID = request.Data.GetOrDefault<long>(k.eid);
            var result = new Dictionary<string, object>();

            var corporation = Corporation.GetOrThrow(corporationEID);

            if (corporation.IsActive && corporation is PrivateCorporation)
            {
                var members = corporation.GetMembersWithAnyRoles(CorporationRole.CorporationDelegate).Select(m => m.character.Id).ToArray();
                result.Add(k.delegateMember, members);
            }

            result.Add(k.corporationEID, corporationEID);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}