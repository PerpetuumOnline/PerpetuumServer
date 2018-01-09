using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionBuyEpBoost : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;
        private readonly MtProductHelper _mtProductHelper;
        private const int EP_BOOST_PERIOD_LENGTH_IN_DAYS = 30;

        public ExtensionBuyEpBoost(IAccountManager accountManager,IAccountRepository accountRepository,MtProductHelper mtProductHelper)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
            _mtProductHelper = mtProductHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);
                var product = _mtProductHelper.GetByAccountTransactionType(AccountTransactionType.EpBoost);

                var wallet = _accountManager.GetWallet(account,AccountTransactionType.EpBoost);
                wallet.Balance -= product.price;
                var e = new AccountTransactionLogEvent(account,AccountTransactionType.EpBoost) {Credit = wallet.Balance, CreditChange = -product.price};
                _accountManager.LogTransaction(e);

                var currentValidUntil = account.ValidUntil ?? DateTime.MaxValue;

                if (currentValidUntil == DateTime.MaxValue || currentValidUntil < DateTime.Now)
                {
                    //validuntil is NULL -- never purchased an EP boost
                    //validUntil is in the past
                
                    var startTime = DateTime.Now;
                    var endTime = DateTime.Now.AddDays(EP_BOOST_PERIOD_LENGTH_IN_DAYS);
                    _accountManager.ExtensionSubscriptionStart(account,startTime, endTime);
                    account.ValidUntil = endTime;
                }
                else
                {
                    //validUntil > now  -- is in the future

                    //extend validuntil
                    var extendedValidUntil = currentValidUntil.AddDays(EP_BOOST_PERIOD_LENGTH_IN_DAYS);

                    _accountManager.ExtensionSubscriptionExtend(account,extendedValidUntil);
                    account.ValidUntil = extendedValidUntil;
                }

                //for stats and stuff
                account.PayingCustomer = true;

                _accountRepository.Update(account);

                Transaction.Current.OnCommited(() =>
                {
                    var info = new Dictionary<string, object>
                    {
                        {k.validUntil, account.ValidUntil}
                    };

                    Message.Builder.FromRequest(request).WithData(info).Send();
                });
                
                scope.Complete();
            }
        }
    }
}
