using System.Collections.Generic;
using Perpetuum.Wallets;

namespace Perpetuum.Accounting
{
    public class AccountWallet : Wallet<int>,IAccountWallet
    {
        private readonly Account _account;
        private readonly AccountTransactionType _transactionType;

        public AccountWallet(Account account,AccountTransactionType transactionType)
        {
            _account = account;
            _transactionType = transactionType;
        }

        protected override void SetBalance(int value)
        {
            _account.Credit = value;
        }

        protected override int GetBalance()
        {
            return _account.Credit;
        }

        protected override void OnBalanceUpdating(int currentCredit, int desiredCredit)
        {
            // handles negative credit as well.
            if (desiredCredit - currentCredit < 0 && desiredCredit < 0)
                throw new PerpetuumException(ErrorCodes.AccountNotEnoughMoney);
        }

        protected override void OnCommited(int startBalance)
        {
            var currentCredit = GetBalance();
            var change = currentCredit - startBalance;

            var info = new Dictionary<string, object>
                    {
                        {k.credit, currentCredit}, 
                        {k.change, change},
                        {k.transactionType, (int)_transactionType}
                    };

            Message.Builder.SetCommand(Commands.AccountUpdateBalance)
                .WithData(info)
                .ToAccount(_account)
                .Send();
        }
    }
}