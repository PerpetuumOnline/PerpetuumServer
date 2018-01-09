using Perpetuum.Accounting;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Sessions;
using Perpetuum.Services.Steam;

namespace Perpetuum.RequestHandlers
{
    public class SignInSteam : SignInRequestHandler
    {
        private readonly ISteamManager _steamManager;
        private readonly IAccountRepository _accountRepository;

        public SignInSteam(ISteamManager steamManager,IRelayStateService relayStateService, ISessionManager sessionManager, IAccountRepository accountRepository, ILoginQueueService loginQueueService) : base(relayStateService, sessionManager, accountRepository, loginQueueService)
        {
            _steamManager = steamManager;
            _accountRepository = accountRepository;
        }

        protected override Account LoadAccount(IRequest request)
        {
            if ( _steamManager.SteamAppID <= 0 )
                throw new PerpetuumException(ErrorCodes.SteamLoginDisabled);

            var encryptedTicket = request.Data.GetOrDefault<byte[]>("encData");
            var accountId = request.Data.GetOrDefault<int>(k.accountID);
            var steamId = _steamManager.GetSteamId(encryptedTicket);
            return _accountRepository.Get(accountId, steamId);
        }
    }
}