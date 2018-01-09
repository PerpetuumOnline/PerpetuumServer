namespace Perpetuum.Accounting
{
    public delegate IAccountWallet AccountWalletFactory(Account account, AccountTransactionType transactionType);
}