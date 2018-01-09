using System;
using Perpetuum.Robots;
using Perpetuum.Timers;

namespace Perpetuum.Zones.Locking.Locks
{
    public delegate void LockEventHandler(Lock @lock);
    public delegate void LockEventHandler<in T>(Lock @lock,T arg);

    public abstract class Lock
    {
        public long Id { get; private set; }

        private LockState _state;

        public LockState State
        {
            get { return _state; }
            private set
            {
                if (_state == value)
                    return;

                _state = value;
                OnChanged();
            }
        }

        public TimeTracker Timer { get; private set; }

        protected Lock(Robot owner)
        {
            Id = FastRandom.NextLong();
            Owner = owner;
        }

        private bool _primary;

        public bool Primary
        {
            get { return _primary; }
            set
            {
                if ( _primary == value )
                    return;

                _primary = value;
                OnChanged();
            }
        }

        public virtual void AcceptVisitor(ILockVisitor visitor)
        {
            visitor.VisitLock(this);
        }

        public Robot Owner { get; private set; }

        public void Update(TimeSpan time)
        {
            switch (State)
            {
                case LockState.Disabled:
                    return;

                case LockState.Inprogress:
                {
                    Timer.Update(time);

                    if (Timer.Expired)
                        State = LockState.Locked;
                    break;
                }
            }
        }
        
        public void Start(TimeSpan lockingTime)
        {
            Timer = new TimeTracker(lockingTime);
            State = LockState.Inprogress;
        }

        public void Cancel()
        {
            State = LockState.Disabled;
        }

        public event LockEventHandler Changed;

        private void OnChanged()
        {
            var changed = Changed;
            changed?.Invoke(this);
        }

        public virtual bool Equals(Lock other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }
    }
}