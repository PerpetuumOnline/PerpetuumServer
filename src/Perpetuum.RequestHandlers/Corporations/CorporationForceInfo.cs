using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationForceInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var corporationEID = request.Data.GetOrDefault<long>(k.eid);
                var publicProfile = request.Data.GetOrDefault<Dictionary<string, object>>(k.publicProfile);

                var corporation = Corporation.GetOrThrow(corporationEID);
                corporation.SetPublicProfile(publicProfile);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}