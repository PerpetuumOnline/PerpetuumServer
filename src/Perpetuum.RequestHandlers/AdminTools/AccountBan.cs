using System;
using Perpetuum.Accounting;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class AccountBan : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISessionManager _sessionManager;

        public AccountBan(IAccountRepository accountRepository,ISessionManager sessionManager)
        {
            _accountRepository = accountRepository;
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var id = request.Data.GetOrDefault<int>(k.accountID);
            var banLength = request.Data.GetOrDefault<int>(k.banLength);
            var banNote = request.Data.GetOrDefault<string>(k.banNote) ?? "";

            var account = _accountRepository.Get(id);
            if (account == null)
                throw new PerpetuumException(ErrorCodes.AccountNotFound);

            account.BanLength = TimeSpan.FromSeconds(banLength);
            account.BanTime = DateTime.Now;
            account.State = AccountState.banned;
            account.BanNote = banNote;
            _accountRepository.Update(account);

            _sessionManager.GetByAccount(account)?.ForceQuit(ErrorCodes.AccountBanned);

            Message.Builder.FromRequest(request).SetData(k.account, account.ToDictionary()).Send();
        }
    }
}
