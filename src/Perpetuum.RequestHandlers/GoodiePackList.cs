using Perpetuum.Accounting;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers
{
    public class GoodiePackList : IRequestHandler
    {
        private readonly GoodiePackHandler _goodiePackHandler;
        private readonly IAccountRepository _accountRepository;

        public GoodiePackList(GoodiePackHandler goodiePackHandler,IAccountRepository accountRepository)
        {
            _goodiePackHandler = goodiePackHandler;
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var account = _accountRepository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

            var result = _goodiePackHandler.ListGoodiePacks(account);
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}