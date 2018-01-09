using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Sparks;

namespace Perpetuum.Services.MarketEngine
{
    public class ExtensionPointActivator : RedeemableItem
    {
        public override void Activate(Account account, Character character)
        {
            AccountManager.AddExtensionPoints(account,ED.Options.ExtensionPoints);

            var e = new AccountTransactionLogEvent(account,AccountTransactionType.EPRedeem)
            {
                Eid = Eid,
                Definition = Definition
            };

            AccountManager.LogTransaction(e);
        }

        public ExtensionPointActivator(IAccountManager accountManager) : base(accountManager)
        {
        }
    }

    public class CreditActivator : RedeemableItem
    {
        public override void Activate(Account account, Character character)
        {
            var wallet = AccountManager.GetWallet(account,AccountTransactionType.CreditRedeem);
            wallet.Balance += ED.Options.Credit;
            var e = new AccountTransactionLogEvent(account,AccountTransactionType.CreditRedeem)
            {
                Credit = wallet.Balance,
                CreditChange = ED.Options.Credit,
                Eid = Eid
            };

            AccountManager.LogTransaction(e);
        }

        public CreditActivator(IAccountManager accountManager) : base(accountManager)
        {
        }
    }

    public class SparkActivator : RedeemableItem
    {
        private readonly SparkHelper _sparkHelper;

        public SparkActivator(IAccountManager accountManager,SparkHelper sparkHelper) : base(accountManager)
        {
            _sparkHelper = sparkHelper;
        }

        public override void Activate(Account account, Character character)
        {
            if ( _sparkHelper.IsSparkUnlocked(character,ED.Options.SparkID))
                throw new PerpetuumException(ErrorCodes.SparkAlreadyUnlocked);

            _sparkHelper.UnlockSpark(character,ED.Options.SparkID);

            var e = new AccountTransactionLogEvent(account,AccountTransactionType.SparkRedeem)
            {
                Eid = Eid
            };

            AccountManager.LogTransaction(e);

            Transaction.Current.OnCommited(() =>
            {
                _sparkHelper.CreateSparksListMessage(character).ToCharacter(character).Send();
            });
        }
    }

    public class Ice : RedeemableItem
    {
        private const int ICE_CREDIT_VALUE = 2400;

        public override void Activate(Account account, Character character)
        {
            var wallet = AccountManager.GetWallet(account,AccountTransactionType.IceRedeem);
            wallet.Balance += ICE_CREDIT_VALUE;
            var e = new AccountTransactionLogEvent(account,AccountTransactionType.IceRedeem)
            {
                Credit = wallet.Balance,
                CreditChange = ICE_CREDIT_VALUE,
                Eid = Eid
            };

            AccountManager.LogTransaction(e);
        }

        public Ice(IAccountManager accountManager) : base(accountManager)
        {
        }
    }
}
