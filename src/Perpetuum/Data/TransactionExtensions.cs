using System;
using System.Diagnostics;
using System.Transactions;
using Perpetuum.Log;

namespace Perpetuum.Data
{
    public static class TransactionExtensions
    {
        public static void OnCommited(this Transaction transaction, Action action)
        {
            OnCompleted(transaction, commited =>
            {
                if (commited)
                    action();
            });
        }

        public static void OnCompleted(this Transaction transaction, Action<bool> action)
        {
            Debug.Assert(transaction != null, "transaction != null");

            var commited = false;
            transaction.EnlistVolatile(() => commited = true,null, () =>
            {
                try
                {
                    action(commited);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            });
        }

        public static void EnlistVolatile(this Transaction transaction, Action onCommit = null, Action onRollback = null, Action onCompleted = null)
        {
            Debug.Assert(transaction != null, "transaction != null");
            var n = AnonymousEnlistmentNotification.Create(onCommit, onRollback, onCompleted);
            transaction.EnlistVolatile(n,EnlistmentOptions.None);
        }
    }
}
