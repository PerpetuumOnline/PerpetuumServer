using System;
using System.Transactions;

namespace Perpetuum.Data
{
    public class AnonymousEnlistmentNotification : IEnlistmentNotification
    {
        private readonly Action _onCommit;
        private readonly Action _onRollback;
        private readonly Action _onCompleted;

        private AnonymousEnlistmentNotification(Action onCommit,Action onRollback,Action onCompleted)
        {
            _onCommit = onCommit;
            _onRollback = onRollback;
            _onCompleted = onCompleted;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            try
            {
                _onCommit();
                enlistment.Done();
            }
            finally
            {
                _onCompleted();
            }
        }

        public void Rollback(Enlistment enlistment)
        {
            try
            {
                _onRollback();
                enlistment.Done();
            }
            finally
            {
                _onCompleted();
            }
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public static AnonymousEnlistmentNotification Create(Action onCommit = null, Action onRollback = null, Action onCompleted = null)
        {
            return new AnonymousEnlistmentNotification(onCommit ?? Stubs.None, onRollback ?? Stubs.None, onCompleted ?? Stubs.None);
        }
    }
}