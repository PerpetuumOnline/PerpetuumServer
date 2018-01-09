using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationTransfer : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var targetCorporationEid = request.Data.GetOrDefault<long>(k.eid);
                var targetCorporation = PrivateCorporation.GetOrThrow(targetCorporationEid);

                var amount = request.Data.GetOrDefault<int>(k.amount);
                var privateCorporation = character.GetPrivateCorporationOrThrow();
                privateCorporation.Transfer(targetCorporation, amount, character);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}