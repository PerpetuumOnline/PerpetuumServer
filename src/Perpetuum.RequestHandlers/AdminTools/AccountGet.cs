using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class AccountGet : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public AccountGet(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }
        public void HandleRequest(IRequest request)
        {
            var id = request.Data.GetOrDefault<int>(k.accountID);
            var account = _accountRepository.Get(id);
            if (account == null)
                throw new PerpetuumException(ErrorCodes.AccountNotFound);

            Message.Builder.FromRequest(request).SetData(k.account, account.ToDictionary()).Send();
        }
    }
}
