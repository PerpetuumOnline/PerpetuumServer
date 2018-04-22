using System;
using System.Threading.Tasks;
using System.Transactions;

namespace Perpetuum.Data
{
    public sealed class Db
    {
        public static Func<DbQuery> DbQueryFactory { get; set; }

        public static DbQuery Query()
        {
            return DbQueryFactory();
        }

        public static DbQuery Query(string commandText)
        {
            return DbQueryFactory().CommandText(commandText);
        }

        public static Task CreateTransactionAsync(Action<TransactionScope> action)
        {
            return Task.Run(() =>
            {
                using (var scope = CreateTransaction())
                {
                    action(scope);
                    scope.Complete();
                }
            }).LogExceptions();
        }

        public static TransactionScope CreateTransaction()
        {
            return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            });
        }
    }
}