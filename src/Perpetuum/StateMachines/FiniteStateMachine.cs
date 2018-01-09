using System;

namespace Perpetuum.StateMachines
{
    public class FiniteStateMachine<T> where T:class,IState
    {
        [CanBeNull]
        public T Current { get; private set; }

        public void ChangeState(T value)
        {
            Current?.Exit();
            Current = value;
            Current?.Enter();
        }

        public void Update(TimeSpan time)
        {
            Current?.Update(time);
        }
    }
}