using Perpetuum.Common.Loggers.Transaction;

namespace Perpetuum.Accounting.Characters
{
    public delegate ICharacterWallet CharacterWalletFactory(Character character, TransactionType transactionType);
}