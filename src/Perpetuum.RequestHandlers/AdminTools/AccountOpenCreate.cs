using Perpetuum.Accounting;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class AccountOpenCreate : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IServerInfoManager _serverInfoManager;

        public AccountOpenCreate(IAccountRepository accountRepository, IServerInfoManager serverInfoManager)
        {
            _accountRepository = accountRepository;
            _serverInfoManager = serverInfoManager;
        }

        public void HandleRequest(IRequest request)
        {
            var email = request.Data.GetOrDefault<string>(k.email);
            var password = request.Data.GetOrDefault<string>(k.password);

            //is the server open?
            var si = _serverInfoManager.GetServerInfo();
            if (!si.IsOpen)
                throw new PerpetuumException(ErrorCodes.InviteOnlyServer);

            var account = new Account
            {
                Email = email,
                Password = password,
                AccessLevel = AccessLevel.normal,
                CampaignId = "{\"host\":\"opencreate\"}"
            };

            if (_accountRepository.Get(account.Email,account.Password) != null)
            {
                Message.Builder.FromRequest(request).WithError(ErrorCodes.AccountAlreadyExists).Send();
                return;
            }

            _accountRepository.Insert(account);

            Message.Builder.FromRequest(request).SetData(k.account, account.ToDictionary()).Send();
        }
    }
}
