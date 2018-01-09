using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationPayOut : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var member = Character.Get(request.Data.GetOrDefault<int>(k.memberID));
                var amount = request.Data.GetOrDefault<long>(k.amount);

                var privateCorporation = character.GetPrivateCorporationOrThrow();
                privateCorporation.PayOut(member, amount, character);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}