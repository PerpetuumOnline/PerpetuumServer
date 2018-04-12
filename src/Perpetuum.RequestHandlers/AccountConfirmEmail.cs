using Perpetuum.Accounting;
using Perpetuum.Host.Requests;


namespace Perpetuum.RequestHandlers
{
    public class AccountConfirmEmail : IRequestHandler
    {

        private readonly IAccountRepository _accountRepository;

        public AccountConfirmEmail(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var accountId = request.Data.GetOrDefault<int>(k.accountID);
            var account = _accountRepository.Get(accountId);
            if (account == null)
            {
                throw new PerpetuumException(ErrorCodes.NoSuchUser);
            }

            account.ForceConfirmEmail();
            Message.Builder.FromRequest(request).WithOk().Send();

        }
    }
}
