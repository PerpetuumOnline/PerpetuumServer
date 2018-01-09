using Perpetuum.Accounting;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers
{
    public class SignIn : SignInRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public SignIn(IRelayStateService relayStateService, ISessionManager sessionManager, IAccountRepository accountRepository, ILoginQueueService loginQueueService) : base(relayStateService, sessionManager, accountRepository, loginQueueService)
        {
            _accountRepository = accountRepository;
        }

        protected override Account LoadAccount(IRequest request)
        {
            var email = request.Data.GetOrDefault<string>(k.email);
            var password = request.Data.GetOrDefault<string>(k.password);
            return _accountRepository.Get(email, password);
        }
    }
}