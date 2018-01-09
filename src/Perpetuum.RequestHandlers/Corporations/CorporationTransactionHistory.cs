using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationTransactionHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);

            Corporation corporation = character.GetPrivateCorporationOrThrow();
            corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.Accountant, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

            var wallet = new CorporationWallet(corporation);

            var result = new Dictionary<string, object>
            {
                { k.corporationEID, corporation.Eid },
                { k.history,corporation.GetTransactionHistory(offsetInDays) },
                { k.wallet, (long)wallet.Balance }
            };

            Message.Builder.FromRequest(request)
                .WithData(result)
                .WrapToResult()
                .Send();
        }
    }
}