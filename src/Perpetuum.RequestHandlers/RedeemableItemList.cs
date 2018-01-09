using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers
{
    public class RedeemableItemList : IRequestHandler
    {
        private readonly GoodiePackHandler _goodiePackHandler;
        private readonly IAccountRepository _accountRepository;

        public RedeemableItemList(GoodiePackHandler goodiePackHandler,IAccountRepository accountRepository)
        {
            _goodiePackHandler = goodiePackHandler;
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var account = _accountRepository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);
                var result = _goodiePackHandler.GetMyRedeemableItems(account);
                Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
                
                scope.Complete();
            }
        }
    }
}