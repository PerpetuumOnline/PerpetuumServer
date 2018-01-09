using Perpetuum.Accounting;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class AccountDelete : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISessionManager _sessionManager;

        public AccountDelete(IAccountRepository accountRepository, ISessionManager sessionManager)
        {
            _accountRepository = accountRepository;
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var id = request.Data.GetOrDefault<int>(k.accountID);

            var account = _accountRepository.Get(id);
            if (account == null)
                throw new PerpetuumException(ErrorCodes.AccountNotFound);

            _sessionManager.GetByAccount(account)?.ForceQuit(ErrorCodes.AccountBanned);

            _accountRepository.Delete(account);

            Message.Builder.FromRequest(request).SetData(k.account, account.ToDictionary()).Send();
        }
    }
}
