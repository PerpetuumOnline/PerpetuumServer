using System;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionFreeLockedEp : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;

        public ExtensionFreeLockedEp(IAccountManager accountManager,IAccountRepository accountRepository)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

                //the locked ep the user wants to release
                var amount = request.Data.GetOrDefault<int>(k.amount);

                (amount <= 0).ThrowIfTrue(ErrorCodes.WTFErrorMedicalAttentionSuggested);

                var currentlockedEp = _accountManager.GetLockedEpByAccount(account);

                (amount > currentlockedEp).ThrowIfTrue(ErrorCodes.InputTooHigh);

                var creditCost = (int) Math.Ceiling(amount/60.0);

                var wallet = _accountManager.GetWallet(account,AccountTransactionType.FreeLockedEp);
                wallet.Balance -= creditCost;

                var e = new AccountTransactionLogEvent(account,AccountTransactionType.FreeLockedEp) { Credit = wallet.Balance, CreditChange = -1 * creditCost };
                _accountManager.LogTransaction(e);

                //inject VS delete

                //delete version
                _accountManager.FreeLockedEp(account,amount);

                _accountRepository.Update(account);

                var character = request.Session.Character;
                var data = _accountManager.GetEPData(account,character);

                Message.Builder.FromRequest(request).WithData(data).Send();
                
                scope.Complete();
            }
        }
    }
}