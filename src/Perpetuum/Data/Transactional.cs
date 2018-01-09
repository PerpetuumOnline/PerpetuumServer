using System.Threading;
using System.Transactions;

namespace Perpetuum.Data
{
    public class Transactional<T> : IEnlistmentNotification
    {
        private T _value;
        private T _txValue;
        private bool _enlisted;
        private readonly AutoResetEvent _are = new AutoResetEvent(true);

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        private T GetValue()
        {
            if (Transaction.Current == null)
            {
                return _value;
            }

            Enlist();
            return _txValue;
        }

        private void SetValue(T newValue)
        {
            Enlist();

            if (Transaction.Current == null)
            {
                _value = newValue;
            }
            else
            {
                _txValue = newValue;
            }
        }

        private void Enlist()
        {
            if (Transaction.Current == null)
                return;

            if (_enlisted)
                return;

            _are.WaitOne();
            _txValue = _value;
            Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            _enlisted = true;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            _value = _txValue;
            _txValue = default(T);
            _are.Set();
            enlistment.Done();
            _enlisted = false;
        }

        public void Rollback(Enlistment enlistment)
        {
            _txValue = default(T);
            _are.Set();
            enlistment.Done();
            _enlisted = false;
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}