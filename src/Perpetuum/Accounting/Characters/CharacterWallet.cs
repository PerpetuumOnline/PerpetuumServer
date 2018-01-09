using System.Collections.Generic;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Wallets;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterWallet : Wallet<double>,ICharacterWallet
    {
        private Character _character;
        private readonly ICharacterCreditService _creditService;
        private readonly TransactionType _transactionType;

        public CharacterWallet(Character character,ICharacterCreditService creditService,TransactionType transactionType)
        {
            _character = character;
            _creditService = creditService;
            _transactionType = transactionType;
        }

        protected override double GetBalance()
        {
            return _creditService.GetCredit(_character.Id);
        }

        protected override void SetBalance(double value)
        {
            _creditService.SetCredit(_character.Id,value);
        }

        protected override void OnBalanceUpdating(double currentCredit, double desiredCredit)
        {
            desiredCredit.ThrowIfLess(0,ErrorCodes.CharacterNotEnoughMoney);
        }

        protected override void OnCommited(double startBalance)
        {
            var currentCredit = GetBalance();
            var change = currentCredit - startBalance;

            var info = new Dictionary<string, object>
                {
                    { k.credit, (long)currentCredit}, 
                    { k.amount,change}, 
                    { k.transactionType, (int)_transactionType }
                };

            Message.Builder.SetCommand(Commands.CharacterUpdateBalance)
                .WithData(info)
                .ToCharacter(_character)
                .Send();
        }
    }
}