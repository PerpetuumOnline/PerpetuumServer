using System;
using Perpetuum.Accounting;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers
{
    public abstract class SignInRequestHandler : IRequestHandler
    {
        private readonly IRelayStateService _relayStateService;
        private readonly ISessionManager _sessionManager;
        private readonly IAccountRepository _accountRepository;
        private readonly ILoginQueueService _loginQueueService;

        protected SignInRequestHandler(IRelayStateService relayStateService,ISessionManager sessionManager,IAccountRepository accountRepository,ILoginQueueService loginQueueService)
        {
            _relayStateService = relayStateService;
            _sessionManager = sessionManager;
            _accountRepository = accountRepository;
            _loginQueueService = loginQueueService;
        }

        public void HandleRequest(IRequest request)
        {
            var account = LoadAccount(request);
            if (account == null)
                throw new PerpetuumException(ErrorCodes.NoSuchUser);
            
            // ignored in standalone
            //account.EmailConfirmed.ThrowIfFalse(ErrorCodes.EmailNotConfirmed);

            var isLoggedIn = account.IsLoggedIn;
            if (isLoggedIn)
            {
                Logger.Info("a logged in account was found, starting disconnect. accountID:" + account.Id);

                var session = _sessionManager.GetByAccount(account);
                session?.ForceQuit(ErrorCodes.NoSimultaneousLoginsAllowed);

                account.IsLoggedIn = false;
                _accountRepository.Update(account);
                throw new PerpetuumException(ErrorCodes.AccountHasBeenDisconnected);
            }

            //account.IsActive.ThrowIfFalse(ErrorCodes.AccountNotPurchased);

            if (account.State.HasFlag(AccountState.banned))
            {
                account.BanTime?.Add(account.BanLength).ThrowIfGreater(DateTime.Now,ErrorCodes.AccountBanned,gex => gex.SetData("banNote",account.BanNote)
                    .SetData("banTime",account.BanTime)
                    .SetData("banLength",(int)account.BanLength.TotalSeconds));

                //auto remove ban if period expired
                account.State &= AccountState.banned;
                _accountRepository.Update(account);
                Logger.Info("ban removed from account: " + account.Id + " email:" + account.Email);
            }

            var hwHash = request.Data.GetOrDefault<string>(k.hash);
            var language = request.Data.GetOrDefault<int>(k.language, 0);
            _loginQueueService.EnqueueAccount(request.Session, account.Id, hwHash, language);
        }

        protected abstract Account LoadAccount(IRequest request);
    }
}