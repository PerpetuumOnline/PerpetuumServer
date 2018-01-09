using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers
{
    public class PBSGetLog : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);
            var zoneId = request.Data.GetOrDefault<int>(k.zoneID);

            var corporationEid = request.Session.Character.CorporationEid;
            
            DefaultCorporationDataCache.IsCorporationDefault(corporationEid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);

            var role = Corporation.GetRoleFromSql(request.Session.Character);

            role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.viewPBS).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

            var history = PBSHelper.GetPBSLog(offsetInDays, corporationEid, zoneId);

            Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> { { k.history, history } }).WrapToResult().Send();
        }
    }
 

}
