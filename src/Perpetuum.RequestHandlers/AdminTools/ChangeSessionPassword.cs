using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class ChangeSessionPassword : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public ChangeSessionPassword(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var currentAccount = _accountRepository.Get(request.Session.AccountId);
            var newPassword = request.Data.GetOrDefault<string>(k.password);

            currentAccount.Password = newPassword;
            _accountRepository.Update(currentAccount);

            Message.Builder.FromRequest(request).SetData(k.account, currentAccount.ToDictionary()).Send();
        }

    }
}
