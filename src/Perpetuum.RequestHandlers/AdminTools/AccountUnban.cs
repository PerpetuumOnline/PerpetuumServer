using System;
using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class AccountUnban : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public AccountUnban(IAccountRepository accountRepository )
        {
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var id = request.Data.GetOrDefault<int>(k.accountID);

            var account = _accountRepository.Get(id);
            if (account == null)
                throw new PerpetuumException(ErrorCodes.AccountNotFound);
           
            account.State = AccountState.normal;
            account.BanTime = null;
            account.BanNote = null;
            account.BanLength = TimeSpan.Zero;
            _accountRepository.Update(account);

            Message.Builder.FromRequest(request).SetData(k.account, account.ToDictionary()).Send();
        }
    }
}
