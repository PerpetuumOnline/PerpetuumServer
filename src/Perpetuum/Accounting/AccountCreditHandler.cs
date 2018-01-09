using System;
using System.Threading;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Threading.Process;

namespace Perpetuum.Accounting
{
    public class AccountCreditHandler : IProcess
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;
        private int _workInProgress;

        public AccountCreditHandler(IAccountManager accountManager,IAccountRepository accountRepository)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            ProcessCreditPayments();
        }

        private void ProcessCreditPayments()
        {
            if ( Interlocked.CompareExchange(ref _workInProgress,1,0) == 1)
                return;

            try
            {
                ProcessCreditQueue();
            }
            finally
            {
                _workInProgress = 0;
            }
        }

        /// <summary>
        /// processes the account credit queue
        /// writes log and informs online affected clients 
        /// </summary>
        private void ProcessCreditQueue()
        {
            var records = Db.Query().CommandText("select * from accountcreditqueue").Execute();

            if (records.Count == 0)
            {
                Logger.DebugInfo("no new account credit record was found");
                return;
            }

            Logger.Info(records.Count + " new account credit records were found");

            //process all new records 
            foreach (var record in records)
            {
                try
                {
                    using (var scope = Db.CreateTransaction())
                    {
                        var accountId = record.GetValue<int>("accountid");

                        Logger.Info("processing account credit queue for accountId:" + accountId);

                        var account = _accountRepository.Get(accountId);
                        if (account == null)
                            continue;

                        var credit = record.GetValue<int>("credit");
                        var id = record.GetValue<int>("id");

                        var wallet = _accountManager.GetWallet(account,AccountTransactionType.Purchase);

                        Logger.Info("accountId:" + accountId + " pre balance:" + wallet.Balance);

                        wallet.Balance += credit;

                        var e = new AccountTransactionLogEvent(account,AccountTransactionType.Purchase) {Credit = wallet.Balance, CreditChange = credit};
                        _accountManager.LogTransaction(e);

                        Db.Query().CommandText("delete accountcreditqueue where id = @id").SetParameter("@id", id).ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);

                        _accountRepository.Update(account);

                        Logger.Info(credit + " credits processed for account " + account.Email + " id:" + account.Id);

                        scope.Complete();
                        Logger.Info(credit + " account got transfered to accountId:" + accountId + " resulting balance:" + wallet.Balance);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }
    }
}
