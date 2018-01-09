using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterTransferCredit : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var amount = request.Data.GetOrDefault<long>(k.amount);
                if (amount <= 0)
                    return;

                var source = request.Session.Character;
                var target = Character.Get(request.Data.GetOrDefault<int>(k.target)).ThrowIfEqual(null, ErrorCodes.CharacterNotFound);

                source.TransferCredit(target, amount);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}