using System;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionFreeAllLockedEpByCommand : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;

        public ExtensionFreeAllLockedEpByCommand(IAccountManager accountManager, IAccountRepository accountRepository)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var accountID = request.Data.GetOrDefault<int>(k.accountID);
                var account = _accountManager.Repository.Get(accountID).ThrowIfNull(ErrorCodes.AccountNotFound);

                //Free all locked EP on account
                var currentlockedEp = _accountManager.GetLockedEpByAccount(account);
                _accountManager.FreeLockedEp(account, currentlockedEp);
                _accountRepository.Update(account);
                scope.Complete();
            }
        }
    }
}